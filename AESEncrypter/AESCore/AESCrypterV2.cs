using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AESCore
{
    public class AESCrypterV2 : IDisposable
    {
        private class EncFileInfo : IDisposable
        {
            public string name;
            public long length;
            public byte[] key;
            public byte[] IV;

            public static EncFileInfo Empty
            {
                get { return new EncFileInfo(null, -1, null, null); }
            }
            

            public EncFileInfo(string name, long length, byte[] key, byte[] IV)
            {
                this.key = key;
                this.name = name;
                this.length = length;
                this.IV = IV;
            }

            public string Serialize()
            {
                var k_s = "";
                foreach (var k in key)
	            {
		             k_s = string.Format("{0}@{1}", k_s, k);
	            }
                var iv_s = "";
                foreach (var i in IV)
	            {
		             iv_s = string.Format("{0}@{1}", iv_s, i);
	            }
                return String.Format("{0}#{1}#{2}#{3}", name, length, k_s, iv_s);
            }

            public static EncFileInfo Parse(string s) 
            {
                var parts = s.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var res = EncFileInfo.Empty;
                res.name = parts[0];
                res.length = long.Parse(parts[1]);
                res.key = 
                    (   
                        from ps in parts[2].Split("@".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                        select byte.Parse(ps)
                    ).ToArray();
            
                res.IV = 
                    (   
                        from ps in parts[3].Split("@".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                        select byte.Parse(ps)
                    ).ToArray();
                return res;
            }

            public void FromString(string s){
                using(var val = Parse(s))
                {
                    this.name = val.name;
                    this.length = val.length;
                    this.IV = val.IV;
                    this.key = val.key;
                }
            }

            public void Dispose()
            {
                name = null;
                length = -1;
                key = null;
                IV = null;
            }
        }

        private const int SizeOfBuffer = 1024 * 8;

        public void Dispose()
        {
            Console.WriteLine("Disposed"); ;
        }

        public string EncryptPath(string key, string in_path, BackgroundWorker worker, string optional_out_path = null)
        {
            try
            {
                EncryptFile(key, in_path, worker, optional_out_path);
            }
            catch (Exception e)
            {
                return string.Format("Errore: {0}", e.Message);
            }
            return "";
        }

        public string DecryptPath(string key, string in_path, BackgroundWorker worker)
        {
            try
            {
                //var b_key = System.Text.Encoding.ASCII.GetBytes(ExpandString(key, 32));
                DecryptFile(key, in_path, worker);
            }
            catch (Exception e)
            {
                return string.Format("Errore: {0}", e.Message);
            }
            return "";
        }

        private string ExpandString(string s, int n)
        {
            if (s.Length == 0)
            {
                throw new Exception("Key must have at least length 1");
            }
            var res = "";
            while (res.Length < n)
            {
                res = string.Format("{0}{1}", res, s);
            }

            return res.Substring(0, n);
        }

        private void CopyStream(Stream input, Stream output)
        {
            using (output)
            using (input)
            {
                byte[] buffer = new byte[SizeOfBuffer];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                }
            }
        }

        private void CopyStreamNotifying(Stream input, Stream output, long in_dim, System.ComponentModel.BackgroundWorker worker)
        {
            using (output)
            using (input)
            {
                byte[] buffer = new byte[SizeOfBuffer];
                int read;
                long progress = 0;
                double percentage = 0;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (worker.CancellationPending)
                    {
                        try
                        {
                            input.Dispose();
                            output.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                        throw new Exception("Aborted by the user");
                    }
                    output.Write(buffer, 0, read);
                    progress += read;
                    double real_percentage = Math.Round(progress / (double)in_dim * 100);
                    if (real_percentage != percentage)
                    {
                        percentage = real_percentage;
                        worker.ReportProgress((int)percentage);
                    }
                }
            }
        }

        private void EncryptFile(string key, string inputPath, BackgroundWorker worker, string outputPath = null)
        {
            var input_name = new FileInfo(inputPath).Name;
            var output_name = outputPath != null ? outputPath : String.Format("{0}\\{1}.enc", new FileInfo(inputPath).DirectoryName, System.Guid.NewGuid().ToString());
            try
            {
                var input_stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
                var output_stream = new FileStream(output_name, FileMode.Create, FileAccess.Write);

                // Essentially, if you want to use RijndaelManaged as AES you need to make sure that:
                // 1.The block size is set to 128 bits
                // 2.You are not using CFB mode, or if you are the feedback size is also 128 bits

                using (var master_alg = new RijndaelManaged { KeySize = 256, BlockSize = 128 })
                {
                    master_alg.GenerateIV();
                    master_alg.GenerateKey();

                    using (var header = new EncFileInfo(input_name, input_stream.Length, master_alg.Key, master_alg.IV))
                    using (var header_alg = new RijndaelManaged { KeySize = 256, BlockSize = 128 })
                    using (var pwd = new Rfc2898DeriveBytes(key, Encoding.ASCII.GetBytes(key.Length < 8 ? ExpandString(key, 8) : key)))
                    {
                        header_alg.Key = pwd.GetBytes(32);
                        header_alg.IV = pwd.GetBytes(16);
                        using (ICryptoTransform h_encryptor = header_alg.CreateEncryptor())
                        // Create the streams used for encryption.
                        using (MemoryStream msEncrypt = new MemoryStream())
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, h_encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(header.Serialize());
                            }
                            var serializer = new BinaryFormatter();
                            serializer.Serialize(output_stream, msEncrypt.ToArray());
                        }
                    }
                    using (var encryptedStream = new CryptoStream(output_stream, master_alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        CopyStreamNotifying(input_stream, encryptedStream, input_stream.Length, worker);
                    }
                }
            }
            catch (Exception e)
            {
                if (File.Exists(output_name))
                {
                    File.Delete(output_name);
                }
                throw;
            }
        }

        private void DecryptFile(string key, string inputPath, BackgroundWorker worker)
        {
            var output_name = "";
            try
            {
                var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

                using (EncFileInfo header = EncFileInfo.Empty)
                {
                    var serializer = new BinaryFormatter();
                    var buff = (byte[])serializer.Deserialize(input);
                    using (var header_alg = new RijndaelManaged { KeySize = 256, BlockSize = 128 })
                    using (var pwd = new Rfc2898DeriveBytes(key, Encoding.ASCII.GetBytes(key.Length < 8 ? ExpandString(key, 8) : key)))
                    {
                        header_alg.Key = pwd.GetBytes(32);
                        header_alg.IV = pwd.GetBytes(16);
                        // Create a decrytor to perform the stream transform.
                        using (ICryptoTransform decryptor = header_alg.CreateDecryptor())
                        {
                            // Create the streams used for decryption.
                            using (MemoryStream msDecrypt = new MemoryStream(buff))
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                header.FromString(srDecrypt.ReadToEnd());
                            }
                        }
                    }
                    var input_name = new FileInfo(inputPath).Name;
                    output_name = String.Format("{0}\\{1}", new FileInfo(inputPath).DirectoryName, header.name);
#if TEST
                    output_name = String.Format("{0}\\Dec{1}", new FileInfo(inputPath).DirectoryName, header[0]);
#endif

                    var output = new FileStream(output_name, FileMode.OpenOrCreate, FileAccess.Write);


                    // Essentially, if you want to use RijndaelManaged as AES you need to make sure that:
                    // 1.The block size is set to 128 bits
                    // 2.You are not using CFB mode, or if you are the feedback size is also 128 bits
                    using (var master_algorithm = new RijndaelManaged { KeySize = 256, BlockSize = 128 })
                    {

                        master_algorithm.Key = header.key;

                        master_algorithm.IV = header.IV;

                        using (var transform = master_algorithm.CreateDecryptor())
                        using (var decryptedStream = new CryptoStream(output, transform, CryptoStreamMode.Write))
                        {
                            CopyStreamNotifying(input, decryptedStream, header.length, worker);
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                if (File.Exists(output_name))
                {
                    File.Delete(output_name);
                }
                throw new InvalidDataException("Password sbagliata. Inserisci quella corretta.");
            }
            catch (Exception ex)
            {
                if (File.Exists(output_name))
                {
                    File.Delete(output_name);
                }
                throw new Exception(ex.Message);
            }
        }
    }
}
