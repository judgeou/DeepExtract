using SharpCompress.Archives.Rar;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DeepExtract
{
    public partial class Form1 : Form
    {
        private const string NEW_LINE = "\r\n";
        private Class1 c1 = new Class1();

        public Form1()
        {
            InitializeComponent();
        }

        private void setArchivePath (string path)
        {
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
            try
            {
                c1.ExtractRecursive(textBox1.Text, textBox2.Text, textBox_pwd.Text, textBox_log);
                textBox_log.AppendText("解压完毕！" + NEW_LINE);
            } catch (Exception ex)
            {
                textBox_log.Text = ex.Message + NEW_LINE + NEW_LINE;
                textBox_log.AppendText(ex.StackTrace);
            }

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
    }
}
