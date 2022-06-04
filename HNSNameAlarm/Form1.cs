using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace HNSNameAlarm
{
    public partial class Form1 : Form
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\HNSAlarm";
        string apikey = "";

        int expblock = 0;
        string expname = "";
        int blocks = 2000;
        int intcheck = 1000000000;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists(path + "\\API.key"))
            {
                apikey apikeyform = new apikey(path);
                apikeyform.ShowDialog();
            }
            StreamReader streamr1 = new StreamReader(path + "\\API.key");
            string apienc = streamr1.ReadLine();
            streamr1.Dispose();
            apikey = StringCipher.Decrypt(apienc, Environment.UserName);
            if (File.Exists(path + "\\settings.txt"))
            {
                StreamReader streamr3 = new StreamReader(path + "\\settings.txt");
                streamr3.ReadLine();
                blocks = int.Parse(streamr3.ReadLine());
                intcheck = int.Parse(streamr3.ReadLine());
                timercheckexp.Interval = int.Parse(streamr3.ReadLine());
                streamr1.Dispose();
            }
            else
            {
                StreamWriter streamw2 = new StreamWriter(path + "\\settings.txt");
                streamw2.WriteLine("Leave in the same format (Blocks till alarm, inverval (in ms) to get names from bob (after the first run through),interval (in ms) to check for name expirations (after the first run through)");
                streamw2.WriteLine("2000");
                streamw2.WriteLine("1000000000");
                streamw2.WriteLine("10000");
                streamw2.Dispose();
            }

            getnextexpiry();
            label2.Text = "Expiry Info\nName: " + expname + "\nBlock: " + expblock.ToString();
            checkexpiry();

            
        }
        public void getnextexpiry()
        {
            // Check last to stop chance of checking while getting new names
            if (File.Exists(path + "\\namesold.txt"))
            {


                StreamReader streamr1 = new StreamReader(path + "\\namesold.txt");
                string lastname = "";
                string nextexpname = "";
                int nextexpiry = int.MaxValue;
                namebox.Items.Clear();
                while (!streamr1.EndOfStream)
                {
                    string line = streamr1.ReadLine();
                    if (line.Contains(":")) //means is the expiry block
                    {

                        int expiry = int.Parse(line.Substring(1));
                        if (expiry < nextexpiry)
                        {
                            nextexpiry = expiry;
                            nextexpname = lastname;

                        }
                    }
                    else
                    {
                        namebox.Items.Add(line);
                        lastname = line;
                    }
                }
                streamr1.Dispose();
                expblock = nextexpiry;
                expname = nextexpname;
            }
        }
        private void timerhide_Tick(object sender, EventArgs e)
        {
            this.Hide();
            timerhide.Stop();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private async void timerchecknames_Tick(object sender, EventArgs e)
        {
            
            if (Process.GetProcessesByName("Bob").Length > 0)
            {
                timercheckexp.Stop();
                timerchecknewnames.Stop();
                if (File.Exists(path + "\\namesold.txt"))
                {
                    File.Delete(path + "\\namesold.txt");
                }
                if (File.Exists(path + "\\names.txt"))
                {
                    File.Move(path + "\\names.txt", path + "\\namesold.txt");
                }

                StreamWriter streamw2 = new StreamWriter(path + "\\names.txt");
                //curl http://x:apikey@127.0.0.1:12039/wallet/cold/name?own=true

                string walletlist = ExecuteCurl(@"curl 'http://x:" + apikey + "@127.0.0.1:12039/wallet'");
                walletlist = walletlist.Replace("[", "");
                walletlist = walletlist.Replace("]", "");
                walletlist = walletlist.Replace("\"", "");
                walletlist = walletlist.Replace(",", "");
                walletlist = walletlist.Trim();
                string[] wallets = walletlist.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                namebox.Items.Clear();

                foreach (string wallet in wallets)
                {
                    string wallettrim = wallet.Trim();

                    string namejson = ExecuteCurl(@"curl 'http://x:" + apikey + "@127.0.0.1:12039/wallet/" + wallettrim + "/name?own=true'");

                    //MessageBox.Show(ExecuteCurl(@"curl 'http://x:" + apikey + "@127.0.0.1:12039/wallet/" + wallettrim + "/name?own=true'"));
                    namejson = namejson.Trim();
                    if (!namejson.Contains("[]") && namejson != "")
                    {
                        string[] lines = namejson.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        foreach (string line in lines)
                        {
                            if (line.Contains("\"name\""))
                            {
                                string[] linesplit = line.Split(':');
                                string linetrim = linesplit[1].Trim();
                                linetrim = linetrim.Substring(1, linetrim.Length - 3);
                                namebox.Items.Add(linetrim);
                                streamw2.WriteLine(linetrim);
                            }
                            if (line.Contains("\"renewalPeriodEnd\""))
                            {
                                string[] linesplit = line.Split(':');
                                string linetrim = linesplit[1].Trim();
                                linetrim = linetrim.Substring(0, linetrim.Length - 1);
                                streamw2.WriteLine(":" + linetrim);
                            }

                        }
                        /*namejson = "{\n\"file\":"+namejson+"}";
                        var root = (JContainer)JToken.Parse(namejson);
                        var list = root.DescendantsAndSelf().OfType<JProperty>().Where(p => p.Name == "name").Select(p => p.Value.Value<string>());
                        //MessageBox.Show(string.Join(",", list.ToArray()));

                        foreach (string name in list)
                        {
                            //MessageBox.Show(name);
                            namebox.Items.Add(name);
                            streamw2.WriteLine(name);
                        }*/
                    }
                }
                streamw2.Dispose();



                if (!File.Exists(path + "\\namesold.txt"))
                {
                    File.Copy(path + "\\names.txt", path + "\\namesold.txt");
                }
                timerchecknewnames.Start();
                timerchecknewnames.Interval = intcheck;
                timercheckexp.Start();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        
        private void timercheckexp_Tick(object sender, EventArgs e)
        {
            checkexpiry();
        }
        bool warned = false;
        private void checkexpiry()
        {
            if (expblock > 0)
            {
                string apiinfo = ExecuteCurl("curl 'https://api.handshakeapi.com/hsd'");
                int index = apiinfo.IndexOf("height");
                apiinfo = apiinfo.Substring(index);
                string[] split = apiinfo.Split(new Char[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                int height = int.Parse(split[1]);

                if (height > expblock - blocks)
                {
                    if (!warned)
                    {
                        //MessageBox.Show(expname + " will expire within the next 2000 blocks (14 days)\nPlease open Bob to renew the name or you could lose it");
                        notifyIcon1.ShowBalloonTip(60, "HNS Alarm", expname + " will expire within the next "+blocks.ToString()+" blocks\nPlease open Bob to renew the name or you could lose it", ToolTipIcon.Warning);
                    }
                }
            }
        }



        public static string ExecuteCurl(string curlCommand, int timeoutInSeconds = 60)
        {
            if (string.IsNullOrEmpty(curlCommand))
                return "";

            curlCommand = curlCommand.Trim();

            // remove the curl keworkd
            if (curlCommand.StartsWith("curl"))
            {
                curlCommand = curlCommand.Substring("curl".Length).Trim();
            }

            // this code only works on windows 10 or higher
            {

                curlCommand = curlCommand.Replace("--compressed", "");

                // windows 10 should contain this file
                var fullPath = System.IO.Path.Combine(Environment.SystemDirectory, "curl.exe");

                if (System.IO.File.Exists(fullPath) == false)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Windows 10 or higher is required to run this application");
                }

                // on windows ' are not supported. For example: curl 'http://ublux.com' does not work and it needs to be replaced to curl "http://ublux.com"
                List<string> parameters = new List<string>();


                // separate parameters to escape quotes
                try
                {
                    Queue<char> q = new Queue<char>();

                    foreach (var c in curlCommand.ToCharArray())
                    {
                        q.Enqueue(c);
                    }

                    StringBuilder currentParameter = new StringBuilder();

                    void insertParameter()
                    {
                        var temp = currentParameter.ToString().Trim();
                        if (string.IsNullOrEmpty(temp) == false)
                        {
                            parameters.Add(temp);
                        }

                        currentParameter.Clear();
                    }

                    while (true)
                    {
                        if (q.Count == 0)
                        {
                            insertParameter();
                            break;
                        }

                        char x = q.Dequeue();

                        if (x == '\'')
                        {
                            insertParameter();

                            // add until we find last '
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \' 
                                if (x == '\\' && q.Count > 0 && q.Peek() == '\'')
                                {
                                    currentParameter.Append('\'');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '\'')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else if (x == '"')
                        {
                            insertParameter();

                            // add until we find last "
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \"
                                if (x == '\\' && q.Count > 0 && q.Peek() == '"')
                                {
                                    currentParameter.Append('"');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '"')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else
                        {
                            currentParameter.Append(x);
                        }
                    }
                }
                catch
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Invalid curl command");
                }

                StringBuilder finalCommand = new StringBuilder();

                foreach (var p in parameters)
                {
                    if (p.StartsWith("-"))
                    {
                        finalCommand.Append(p);
                        finalCommand.Append(" ");
                        continue;
                    }

                    var temp = p;

                    if (temp.Contains("\""))
                    {
                        temp = temp.Replace("\"", "\\\"");
                    }
                    if (temp.Contains("'"))
                    {
                        temp = temp.Replace("'", "\\'");
                    }

                    finalCommand.Append($"\"{temp}\"");
                    finalCommand.Append(" ");
                }


                using (var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "curl.exe",
                        Arguments = finalCommand.ToString(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.SystemDirectory
                    }
                })
                {
                    proc.Start();

                    proc.WaitForExit(timeoutInSeconds * 1000);

                    return proc.StandardOutput.ReadToEnd();
                }
            }
        }
    }

    public static class StringCipher
    {
        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}
