using SharpCompress;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DeepExtract
{
    internal class Class1
    {
        private const int BUFFER_SIZE = 1024 * 512;

        private long totalBytes;
        private long totalBytesRead;
        public IList<string> extractedFileList = new List<string>();
        private Stack<string> archiveQueue = new Stack<string>();
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
                // FileStream stream = File.OpenRead(fileName);
                return SevenZipArchive.Open(fileName, options);
            } else if (archiveType == ArchiveType.Zip) {
                // FileStream stream = File.OpenRead(fileName);
                return ZipArchive.Open(fileName, options);
            } else if (archiveType == ArchiveType.RAR)
            {
                // FileStream stream = File.OpenRead(fileName);
                return RarArchive.Open(fileName, options);
            }
            {
                throw new Exception("不支持的压缩包格式: " + fileName);
            }
        }

        public void ResetCounter() {
            this.totalBytes = 0;
            this.totalBytesRead = 0;
            archiveQueue.Clear();
        }

        public void ExtractRecursive (string fileName, string outputName, string[] pwdArray, BackgroundWorker worker, int maxdepth, int beginPasswordIndex)
        {
            this.worker = worker;
            var password = pwdArray[beginPasswordIndex % pwdArray.Length];
            using (var archive = OpenArchive(fileName, new ReaderOptions()
            {
                Password = password.Length > 0 ? password : null,
                LeaveStreamOpen = false,
                LookForHeader = true,
            }))
            {
                var entries = archive.Entries;
                var outputNameDepth = outputName; // Path.Combine(outputName);

                this.totalBytes += archive.TotalUncompressSize;

                using (var reader = archive.ExtractAllEntries())
                {
                    // reader.FilePartExtractionBegin += Archive_EntryExtractionBegin;
                    // reader.EntryExtractionEnd += Archive_EntryExtractionEnd;
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            using (var entryStream = reader.OpenEntryStream())
                            {
                                extractedFileList.Add("Extracting " + reader.Entry.Key + "...");
                                var outputFilePath = Path.Combine(outputNameDepth, reader.Entry.Key);
                                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                                using (var writer = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                                {
                                    CopyStreamWithProgress(entryStream, writer);

                                    if (worker.CancellationPending)
                                    {
                                        reader.Cancel();
                                        break;
                                    }
                                }

                                if (maxdepth > 1)
                                {
                                    if (DetectArchiveType(outputFilePath) != ArchiveType.Unknown)
                                    {
                                        // ExtractRecursive(outputFilePath, outputName, pwdArray, worker, maxdepth - 1, beginPasswordIndex + 1);
                                        archiveQueue.Push(outputFilePath);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (archiveQueue.Count >= 1)
            {
                var archiveFilePath = archiveQueue.Pop();
                ExtractRecursive(archiveFilePath, outputName, pwdArray, worker, maxdepth - 1, beginPasswordIndex + 1);
            }
        }

        private void CopyStreamWithProgress(Stream sourceStream, Stream destinationStream)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            // long totalBytes = sourceStream.Length;
            int bytesRead;

            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                destinationStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                var p = (double)totalBytesRead / totalBytes * 100;
                worker.ReportProgress((int)(p > 100 ? 100 : p));
                // this.compressedBytesRead = e.CompressedBytesRead;

                if (worker.CancellationPending)
                {
                    break;
                }
            }
        }

        
    }
}
