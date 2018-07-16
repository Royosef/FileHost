using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using FileHost.Models;

namespace FileHost.FilesManagement
{
    public class ItemsDownloader
    {
        private DataAccess DataAccess { get; } = DataAccess.Instance;
        private ItemsLoader ItemsLoader { get; } = new ItemsLoader();

        public async Task DownloadItems(List<Item> items, string name)
        {
            await CreateZip(name, async archive =>
            {
                foreach (var item in items)
                {
                    switch (item)
                    {
                        case FolderItem folderItem:
                            await CreateFolderEntry(folderItem, archive);

                            break;
                        case FileItem fileItem:
                            await CreateFileEntry(fileItem, archive);

                            break;
                    }
                }
            });
        }

        public async Task DownloadFile(FileItem fileItem, string name)
        {
            var bytes = await DownloadFileItem(fileItem);

            using (var stream = new FileStream(name, FileMode.Create, FileAccess.Write))
            using (var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(bytes);
            }
        }

        public async Task DownloadFolder(FolderItem folderItem, string name)
        {
            await CreateZip(name, async archive => { await CreateFolderEntry(folderItem, archive); });
        }

        private async Task CreateZip(string name, Func<ZipArchive, Task> createEnteriesAsyncFunc)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    await createEnteriesAsyncFunc(archive);
                }

                using (var fileStream = new FileStream(name, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }

        private async Task CreateFileEntry(FileItem fileItem, ZipArchive archive)
        {
            var fileBytes = await DownloadFileItem(fileItem);
            var fileEntry = archive.CreateEntry(fileItem.Name);

            WriteBytesToEntry(fileBytes, fileEntry);
        }

        private async Task<byte[]> DownloadFileItem(FileItem fileItem)
        {
            var fileResult = await DataAccess.Get($"{fileItem.Id}/{fileItem.Name}", fileItem.Revision);
            var fileByteArray = await fileResult.Content.ReadAsByteArrayAsync();

            return fileByteArray;
        }

        private async Task CreateFolderEntry(FolderItem folderItem, ZipArchive archive)
        {
            var files = await DownloadFolderItem(folderItem);

            foreach (var filePair in files)
            {
                var filePairEntry = archive.CreateEntry($"{folderItem.Name}/{filePair.Key}");

                WriteBytesToEntry(filePair.Value, filePairEntry);
            }
        }

        private async Task<Dictionary<string, byte[]>> DownloadFolderItem(FolderItem folderItem)
        {
            var fileItems = await ItemsLoader.GetFolderFiles(folderItem);
            var filesByetesArrays = new Dictionary<string, byte[]>();

            foreach (var file in fileItems)
            {
                filesByetesArrays.Add(file.Name, await DownloadFileItem(file));
            }

            return filesByetesArrays;
        }

        private void WriteBytesToEntry(byte[] bytes, ZipArchiveEntry entry)
        {
            using (var entryStream = entry.Open())
            using (var binaryWriter = new BinaryWriter(entryStream))
            {
                binaryWriter.Write(bytes);
            }
        }
    }
}