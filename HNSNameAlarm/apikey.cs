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
using System.Security.Cryptography;


namespace HNSNameAlarm
{
    public partial class apikey : Form
    {
        string path = "";
        public apikey(String Enviromentpath)
        {
            InitializeComponent();
            path = Enviromentpath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("You need to add an API key");
                return;
            }
            string APIenc = StringCipher.Encrypt(textBox1.Text, Environment.UserName);
            StreamWriter streamw1 = new StreamWriter(path + "\\API.key");
            streamw1.Write(APIenc);
            streamw1.Dispose();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To get your API key, open Bob and go to settings via the cog icon.\nGo to \"Network & Connection\"\nCopy your API key and paste it here.", "How to find API Key");
        }
    }
}
