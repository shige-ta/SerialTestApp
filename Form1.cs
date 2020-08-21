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
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;


namespace SerialTestApp
{
    public partial class Form1 : Form
    {
        private SerialPort myPort;
        Task r;


        public Form1()
        {

            InitializeComponent();

            //ユーザーがサイズを変更できないようにする
            //最大化、最小化はできる
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            //最大サイズと最小サイズを現在のサイズに設定する
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;

            // 設定ファイル初期化
            if (Properties.Settings.Default.CRLF == "")
            {
                Properties.Settings.Default.CRLF = "CRのみ";
            }
            if (Properties.Settings.Default.bps == "")
            {
                Properties.Settings.Default.bps = "9600 bps";
            }

            //無効化
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            button1.Enabled = false;

            // 接続可能なCOMリスト作成
            ManagementClass device = new ManagementClass("Win32_SerialPort");
            foreach (ManagementObject port in device.GetInstances())
            {
                comboBox1.Items.Add((string)port.GetPropertyValue("DeviceID") + "  " +
                                    (string)port.GetPropertyValue("Caption"));
            }

            // comboBox項目位置初期化
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            // 文末コード
            comboBox2.Items.Add("改行なし");
            comboBox2.Items.Add("LFのみ");
            comboBox2.Items.Add("CRのみ");
            comboBox2.Items.Add("CRおよびLF");

            for(int i= 0;i<comboBox2.Items.Count;i++)
            {
                if (comboBox2.Items[i].ToString() == Properties.Settings.Default.CRLF)
                {
                    comboBox2.SelectedIndex = i;
                }
            }

            // 周波数
            comboBox3.Items.Add("300 bps");
            comboBox3.Items.Add("1200 bps");
            comboBox3.Items.Add("2400 bps");
            comboBox3.Items.Add("4800 bps");
            comboBox3.Items.Add("9600 bps");
            comboBox3.Items.Add("19200 bps");
            comboBox3.Items.Add("38400 bps");
            comboBox3.Items.Add("57600 bps");
            comboBox3.Items.Add("115200 bps");

            for (int i = 0; i < comboBox3.Items.Count; i++)
            {
                if (comboBox3.Items[i].ToString() == Properties.Settings.Default.bps)
                {
                    comboBox3.SelectedIndex = i;
                }
            }
        }

        private void send()
        {

            try
            {
                string endst = "";

                switch (comboBox2.Text)
                {
                    case "改行なし":
                        endst = "";
                        break;
                    case "LFのみ":
                        endst = "\n";
                        break;
                    case "CRのみ":
                        endst = "\r";
                        break;
                    case "CRおよびLF":
                        endst = "\r\n";
                        break;
                }

                // string を byteに変換
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(textBox1.Text + endst);
                // 送信  
                myPort.Write(buffer, 0, buffer.Length);

                //            myPort.Close();
            }
            catch (Exception) {}

        }




        // 送信
        private void button1_Click(object sender, EventArgs e)
        {
            send();
        }

        // 選択したCOMに接続する
        private void button2_Click(object sender, EventArgs e)
        {
            if( comboBox1.Items.Count == 0)
            {
                MessageBox.Show("接続できません");
                return;
            }

            int posi;
            posi = comboBox1.Text.IndexOf(" ");
            string PortName = comboBox1.Text.Substring(0, posi);
            int BaudRate;

            // 周波数
            string s = comboBox3.Text;
            Regex re = new Regex(@"[^0-9]");
            BaudRate = Int32.Parse(re.Replace(s, ""));

            Parity Parity = Parity.None;
            int DataBits = 8;
            StopBits StopBits = StopBits.One;



            myPort =
            new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);

            try
            {
                myPort.Open();

                r = ReceiveData();

                //有効化
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                button1.Enabled = true;

                button2.Enabled = false;
            }
            catch
            {
            
            }
        }

        // 半角英数しか入力できないようにする
        //        Regex notIntReg = new Regex(@"[^a-zA-Z0-9\s]");
        Regex notIntReg = new Regex(@"[^a-zA-Z0-9!-/\s]");

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int i = textBox1.SelectionStart;
            textBox1.Text = notIntReg.Replace(textBox1.Text, "");
            textBox1.SelectionStart = i;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            //押されたキーがエンターキーかどうかの条件分岐
            if (e.KeyCode == Keys.Enter)
            {
                send();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.CRLF = comboBox2.Text;
            Properties.Settings.Default.bps = comboBox3.Text;
            Properties.Settings.Default.Save();

            try
            {
                if (myPort.IsOpen)
                {
                    myPort.Close();
                    r.Dispose();
                }
            }
            catch
            { }


        }

        private void バージョンToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("シリアルモニタ　バージョン：0.7b");
        }

        async Task ReceiveData()
        {
            await Task.Run(() => {
                while (true)
                {
                    if (myPort == null)
                    {
                        return;
                    }
                    try
                    {
                        string dt;
                        dt = myPort.ReadLine();

                        this.Invoke(new MethodInvoker(() => textBox2.Text += dt + "\r\n"));
                    }
                    catch (Exception) { }
                }
            });
        }
    }
}