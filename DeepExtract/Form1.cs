using SharpCompress.Archives.Rar;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DeepExtract
{
    public partial class Form1 : Form
    {
        private Class1 c1 = new Class1();
        private BackgroundWorker workerExtract;
        private string filePath = "passwordHistory.txt";
        private List<string> passwordHistory = new List<string>();

        public Form1()
        {
            InitializeComponent();

            LoadPasswordHistory();
        }

        private void setArchivePath (string path)
        {
            if (path == null) {
                return;
            }
            string fileName = textBox1.Text = path;
            string parent = Path.GetDirectoryName(fileName);
            string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
            textBox2.Text = Path.Combine(parent, fileNameShort + "_output");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            setArchivePath(c1.SelectFile()); 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = c1.SelectFolder();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            workerExtract = new BackgroundWorker();
            workerExtract.DoWork += WorkerExtract_DoWork;
            workerExtract.ProgressChanged += WorkerExtract_ProgressChanged;
            workerExtract.RunWorkerCompleted += WorkerExtract_RunWorkerCompleted;
            workerExtract.WorkerReportsProgress = true;
            workerExtract.WorkerSupportsCancellation = true;
            button1.Enabled = false;
            textBox_log.Text = "";
            c1.extractedFileList.Clear();

            workerExtract.RunWorkerAsync();

        }

        private void WorkerExtract_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBox_log.Text = e.Error.Message + Environment.NewLine + Environment.NewLine;
                textBox_log.AppendText(e.Error.StackTrace);
            } else
            {
                textBox_log.AppendText("解压完毕！" + Environment.NewLine);
            }

            button1.Enabled = true;
        }

        private void ScrollToBottom(TextBox textBox)
        {
            // Get the length of the text in the TextBox
            int length = textBox.TextLength;

            // Scroll to the end of the text
            textBox.SelectionStart = length;
            textBox.SelectionLength = 0;
            textBox.ScrollToCaret();
        }

        private void WorkerExtract_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0)
            {
                progressBar1.Value = e.ProgressPercentage;
            }
            if (e.UserState != null)
            {
                textBox_log.AppendText((string)e.UserState);
                textBox_log.AppendText(Environment.NewLine);
            }

            ScrollToBottom(textBox_log);
        }

        private void WorkerExtract_DoWork(object sender, DoWorkEventArgs e)
        {
            var pwdArray = textBox_pwd.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            c1.ResetCounter();
            c1.ExtractRecursive(textBox1.Text, textBox2.Text, pwdArray, workerExtract, (int)numericUpDown1.Value, 0);

            foreach (var pwd in pwdArray)
            {
                AddPasswordToHistory(pwd);
            }
            OpenDirectory(textBox2.Text);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                setArchivePath(paths[0]);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            } else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // stop
        private void button4_Click(object sender, EventArgs e)
        {
            workerExtract.CancelAsync();
        }

        public void OpenDirectory(string path)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                }
            };
            process.Start();
        }

        private void LoadPasswordHistory()
        {
            if (Properties.Settings.Default.解压密码 != null)
            {
                foreach (string password in Properties.Settings.Default.解压密码)
                {
                    // 这里可以将历史密码加载到列表中
                    passwordHistory.Add(password);
                }
            }
        }

        private void SavePasswordHistory()
        {
            if (Properties.Settings.Default.解压密码 == null)
            {
                Properties.Settings.Default.解压密码 = new StringCollection();
            }

            Properties.Settings.Default.解压密码.Clear();
            foreach (string password in passwordHistory)
            {
                Properties.Settings.Default.解压密码.Add(password);
            }

            // 保存到用户设置
            Properties.Settings.Default.Save();
        }

        private void AddPasswordToHistory(string password)
        {
            if (!passwordHistory.Contains(password))
            {
                passwordHistory.Add(password);
                SavePasswordHistory();
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // 清空之前的菜单项
            contextMenuStrip1.Items.Clear();

            if (passwordHistory.Count == 0)
            {
                var item = new ToolStripMenuItem("无历史解压密码");
                item.Enabled = false;
                contextMenuStrip1.Items.Add(item);
            } else
            {
                // 为每个历史密码创建一个菜单项
                foreach (var password in passwordHistory)
                {
                    var item = new ToolStripMenuItem(password);
                    item.Click += Item_Click;
                    contextMenuStrip1.Items.Add(item);
                }
            }
     
        }

        private void Item_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            textBox_pwd.AppendText(item.Text + Environment.NewLine);
        }
    }
}
