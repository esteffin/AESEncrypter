using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace AESCore
{
    public class AESCrypter : IDisposable
    {
        #region Fields and Properties
        private string GUID = "fe0753db-78b2-49f2-b850-cb1d6a29ef74";
        private string HiddenIV = "71;230;224;26;113;235;76;87;69;44;167;66;227;111;234;220;217;55;102;17;13;143;176;64;246;226;80;149;18;200;114;255;195;107;145;76;57;228;172;76;40;100;138;45;100;113;50;219;188;202;121;40;74;60;99;156;35;206;118;240;88;231;232;38";
        private byte[] ZeroIV = new byte[16];

        private byte[] InnerIV 
            {
                get
                {                    
                    var internal_key = ReduceXOR(System.Text.Encoding.ASCII.GetBytes(GUID), 32);
                    return DeserializeBytes(DecryptStringFromBytes_Aes(DeserializeBytes(HiddenIV), internal_key, ZeroIV));
                }
            }
        //}
        private const int SizeOfBuffer = 1024 * 8;
        #endregion

        #region Private methods

        private byte[] DeserializeBytes(string data) 
        {
            return
                (
                    from val in data.Split(";".ToCharArray())
                    select byte.Parse(val)
                ).ToArray();
        }

        private byte[] GetRandomIV() 
        {
            var rnd = new RNGCryptoServiceProvider();

            var res = new byte[16];
            rnd.GetBytes(res);
            return res;
        }

        private string SerializeBytes(byte[] item) 
        {
            string res = String.Format("{0}", item[0]);
            for (int i = 1; i < item.Length; i++)
            {
                res = string.Format("{0};{1}", res, item[i]);
            }
            return res;
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

        private byte[] ReduceXOR(byte[] bytes, int n)
        {
            var res = new byte[n];
            for (int i = 0; i < bytes.Length; i++)
            {
                res[i % n] = (byte)(res[i % n] ^ bytes[i]);
            }
            return res;
        }

        private byte[] GetKey(string key)
        {
            var internal_key = ReduceXOR(System.Text.Encoding.ASCII.GetBytes(GUID), 32);
            var res = ReduceXOR(EncryptStringToBytes_Aes(key, internal_key, InnerIV), 32);
            return res;
        }

        private byte[] GetInternalKey(string key)
        {
            var internal_key = ReduceXOR(System.Text.Encoding.ASCII.GetBytes(GUID), 32);
            var res = ReduceXOR(EncryptStringToBytes_Aes(key, internal_key, InnerIV), 32);
            return res;
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

        #endregion

        #region Encription methods
        private void EncryptFile(byte[] key, string inputPath, BackgroundWorker worker, string outputPath = null)
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

                var algorithm = Aes.Create(); //new RijndaelManaged { KeySize = 256, BlockSize = 128 };
                //var key = key; //new Rfc2898DeriveBytes(password, Encoding.ASCII.GetBytes(Salt));

                algorithm.Key = key;
                algorithm.IV = GetRandomIV();

                var header = string.Format("{0}#{1}#{2}", input_name, input_stream.Length, SerializeBytes(algorithm.IV));

                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(output_stream, EncryptStringToBytes_Aes(header, key, InnerIV));
                }

                using (var encryptedStream = new CryptoStream(output_stream, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    CopyStreamNotifying(input_stream, encryptedStream, input_stream.Length, worker);
                }
            }
            catch (Exception)
            {
                if (File.Exists(output_name))
                {
                    File.Delete(output_name);
                }                
                throw;
            }
        }

        private void DecryptFile(byte[] key, string inputPath, BackgroundWorker worker)
        {
            var output_name = "";
            try
            {
                var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

                string[] header;

                {
                    var serializer = new BinaryFormatter();
                    var buff = (byte[])serializer.Deserialize(input);
                    header = DecryptStringFromBytes_Aes(buff, key, InnerIV).Split("#".ToCharArray());
                    //input.Position = input.Length - long.Parse(header[1]) - 1;
                }

                var input_name = new FileInfo(inputPath).Name;

                output_name = String.Format("{0}\\{1}", new FileInfo(inputPath).DirectoryName, header[0]);
#if TEST
                output_name = String.Format("{0}\\Dec{1}", new FileInfo(inputPath).DirectoryName, header[0]);
#endif


                var output = new FileStream(output_name, FileMode.OpenOrCreate, FileAccess.Write);



                // Essentially, if you want to use RijndaelManaged as AES you need to make sure that:
                // 1.The block size is set to 128 bits
                // 2.You are not using CFB mode, or if you are the feedback size is also 128 bits
                var algorithm = Aes.Create(); //new RijndaelManaged { KeySize = 256, BlockSize = 128 };

                algorithm.Key = key;//.GetBytes(algorithm.KeySize / 8);

                algorithm.IV = DeserializeBytes(header[2]);

                using (var decryptedStream = new CryptoStream(output, algorithm.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    CopyStreamNotifying(input, decryptedStream, long.Parse(header[1]), worker);
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

        private byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        private string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    // aesAlg.IV = GetIV(GUID.GetHashCode());
                    aesAlg.IV = IV;

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new InvalidDataException("Password sbagliata. Inserisci quella corretta.");
            }
            return plaintext;
        }

        #endregion

        #region Public methods

        public string EncryptString(string key, string plain_text)
        {
            var inner_key = GetKey(key);
            var encrypted = EncryptStringToBytes_Aes(plain_text, inner_key, ZeroIV);
            return SerializeBytes(encrypted);
        }

        public string DecryptString(string key, string ciphered_text)
        {
            var ciphered = DeserializeBytes(ciphered_text);
            var inner_key = GetKey(key);
            return DecryptStringFromBytes_Aes(ciphered, inner_key, ZeroIV);
        }

        public string EncryptPath(string key, string in_path, BackgroundWorker worker, string optional_out_path = null)
        {
            try
            {
                var b_key = GetKey(ExpandString(key, 32));
                EncryptFile(b_key, in_path, worker, optional_out_path);
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
                var b_key = GetKey(ExpandString(key, 32));
                DecryptFile(b_key, in_path, worker);
            }
            catch (Exception e)
            {                
                return string.Format("Errore: {0}", e.Message);
            }
            return "";
        }

        public void Dispose()
        {
            this.GUID = "";
            this.HiddenIV = "";
        }
        #endregion

    }
}
