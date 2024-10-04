using SharpCompress.Archives.Rar;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DeepExtract
{
    public partial class Form1 : Form
    {
        private const string NEW_LINE = "\r\n";
        private Class1 c1 = new Class1();
        private BackgroundWorker workerExtract;

        public Form1()
        {
            InitializeComponent();
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
                textBox_log.Text = e.Error.Message + NEW_LINE + NEW_LINE;
                textBox_log.AppendText(e.Error.StackTrace);
            } else
            {
                textBox_log.AppendText("解压完毕！" + NEW_LINE);
            }

            button1.Enabled = true;
        }

        private void WorkerExtract_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            var logtext = String.Join("\r\n", c1.extractedFileList);
            textBox_log.Text = logtext + "\r\n";
        }

        private void WorkerExtract_DoWork(object sender, DoWorkEventArgs e)
        {
            c1.ExtractRecursive(textBox1.Text, textBox2.Text, textBox_pwd.Text, workerExtract);
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
    }
}
