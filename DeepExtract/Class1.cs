using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepExtract
{
    internal class Class1
    {
        public const string NEW_LINE = "\r\n";
        private const int BUFFER_SIZE = 1024 * 512;

        private long totalBytes;
        private long compressedBytesRead;
        private long totalCompressedBytesRead;
        public IList<string> extractedFileList = new List<string>();
        private BackgroundWorker worker;

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

        public IArchive OpenArchive (string fileName, ReaderOptions options)
        {
            var archiveType = DetectArchiveType(fileName);
            if (archiveType == ArchiveType.SevenZip)
            {
                FileStream stream = File.OpenRead(fileName);
                return SevenZipArchive.Open(stream, options);
            } else if (archiveType == ArchiveType.Zip) {
                FileStream stream = File.OpenRead(fileName);
                return ZipArchive.Open(stream, options);
            } else if (archiveType == ArchiveType.RAR)
            {
                FileStream stream = File.OpenRead(fileName);
                return RarArchive.Open(stream, options);
            }
            {
                throw new Exception("不支持的压缩包格式: " + fileName);
            }
        }

        public void ResetCounter() {
            this.totalBytes = 0;
            this.compressedBytesRead = 0;
            this.totalCompressedBytesRead = 0;
        }

        public void ExtractRecursive (string fileName, string outputName, string password, BackgroundWorker worker, int maxdepth)
        {
            this.worker = worker;
            using (var archive = OpenArchive(fileName, new ReaderOptions()
            {
                Password = password.Length > 0 ? password : null,
                LeaveStreamOpen = false
            }))
            {
                var entries = archive.Entries;
                var outputNameDepth = outputName; // Path.Combine(outputName);

                this.totalBytes += archive.TotalSize;
                this.compressedBytesRead = 0;
                this.totalCompressedBytesRead = 0;
                archive.CompressedBytesRead += Archive_CompressedBytesRead;
                archive.EntryExtractionBegin += Archive_EntryExtractionBegin;
                archive.EntryExtractionEnd += Archive_EntryExtractionEnd;

                foreach (var entry in entries)
                {
                    if (!entry.IsDirectory && !worker.CancellationPending)
                    {
                        var outputFilePath = Path.Combine(outputNameDepth, entry.Key);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                        entry.WriteToFile(outputFilePath);

                        if (DetectArchiveType(outputFilePath) != ArchiveType.Unknown)
                        {
                            if (maxdepth > 1)
                            {
                                ExtractRecursive(outputFilePath, outputName, password, worker, maxdepth - 1);
                            }
                        }
                    }
                }
            }
        }

        private void Archive_EntryExtractionEnd(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            this.totalCompressedBytesRead += this.compressedBytesRead;
        }

        private void Archive_EntryExtractionBegin(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            extractedFileList.Add("Extracting " + e.Item.Key + "...");
            // textBox_log.AppendText("Extracting " + e.Item.Key + "..." + NEW_LINE);
        }

        private void Archive_CompressedBytesRead(object sender, CompressedBytesReadEventArgs e)
        {
            var p = ((double)e.CompressedBytesRead + this.totalCompressedBytesRead) / totalBytes * 100;
            worker.ReportProgress((int)(p > 100 ? 100 : p));
            this.compressedBytesRead = e.CompressedBytesRead;
        }
    }
}
