using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepExtract
{
    internal class Class1
    {
        public enum ArchiveType
        {
            RAR,
            SevenZip,
            Zip,
            Unknown
        }

        public string SelectFile ()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Compressed Files |*";
                ofd.Title = "选择一个压缩包";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    return ofd.FileName;
                }
                else
                {
                    return null;
                }
            }
        }

        public string SelectFolder () {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择一个文件夹";
                folderDialog.ShowNewFolderButton = true; // 允许用户创建新文件夹

                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    return folderDialog.SelectedPath;
                } else
                {
                    return null;
                }
            }
        }

        public ArchiveType DetectArchiveType(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);

                // Check for RAR
                if (buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21)
                {
                    return ArchiveType.RAR;
                }

                // Check for 7Z
                if (buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF && buffer.Length == 4)
                {
                    fileStream.Read(buffer, 0, 4);
                    if (buffer[0] == 0x27 && buffer[1] == 0x1C)
                    {
                        return ArchiveType.SevenZip;
                    }
                }

                // Check for ZIP
                if (buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04)
                {
                    return ArchiveType.Zip;
                }
            }

            return ArchiveType.Unknown;
        }

        public void ExtractRecursive (string fileName, string outputName, string password, TextBox textBox_log, int depth = 0)
        {
            var archiveType = DetectArchiveType (fileName);
            using (FileStream stream = File.OpenRead(fileName))
            {
                if (archiveType == ArchiveType.SevenZip)
                {
                    using (var archive = SevenZipArchive.Open(stream, new ReaderOptions()
                    {
                        Password = password.Length > 0 ? password : null
                    }))
                    {
                        var entries = archive.Entries;
                        var outputNameDepth = Path.Combine(outputName, "depth_" + depth);

                        foreach (var entry in entries)
                        {
                            if (!entry.IsDirectory)
                            {
                                var outputFilePath = Path.Combine(outputNameDepth, entry.Key);
                                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                                textBox_log.AppendText("Extracting " + outputFilePath + "..." + "\r\n");
                                entry.WriteToFile(outputFilePath);

                                if (DetectArchiveType(outputFilePath) != ArchiveType.Unknown)
                                {
                                    ExtractRecursive(outputFilePath, outputName, password, textBox_log, depth + 1);
                                }
                            }
                        }
                    }
                }
                else {
                    textBox_log.Text = "不支持的压缩包格式";
                }
            }
        }
    }
}
