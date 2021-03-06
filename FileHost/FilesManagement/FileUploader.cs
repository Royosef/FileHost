﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using FileHost.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileHost.FilesManagement
{
    public class FileUploader
    {
        private DataAccess DataAccess { get; } = DataAccess.Instance;
        private ItemsDeleter ItemsDeleter { get; } = new ItemsDeleter();

        public async Task<List<FileItem>> Upload(List<string> files, string currentFolderId, List<string> existingFilesNames)
        {
            if (!files.Any())
            {
                return new List<FileItem>();
            }

            var uploadedFiles = new List<FileItem>();

            foreach (var fileName in files)
            {
                var fileItem = await CreateFileDocument(fileName, currentFolderId, existingFilesNames);

                if (fileItem != null)
                {
                    existingFilesNames.Add(Path.GetFileNameWithoutExtension(fileItem.Name));
                    uploadedFiles.Add(fileItem);
                }
            }

            return uploadedFiles;
        }

        private async Task<FileItem> CreateFileDocument(string file, string folderId, IEnumerable<string> existingFilesNames)
        {
            var fileName = Path.GetFileName(file);
            fileName = RenameFileWithExistingName(fileName, existingFilesNames);

            try
            {
                var bin = ReadBytes(file);

                var fileItem = new FileItem
                {
                    Data = bin,
                    Name = fileName,
                    ContainingFolderId = folderId,
                    Size = bin.Length
                };

                var docResult = await DataAccess.PostAsJson(fileItem, string.Empty);

                if (!docResult.IsSuccessStatusCode)
                {
                    throw new Exception($"Error: Some error occurred.\nFile: {fileItem.Name}");
                }

                var doc = JsonConvert.DeserializeAnonymousType(await docResult.Content.ReadAsStringAsync(), new { Id = string.Empty, Rev = string.Empty });
                fileItem.Id = doc.Id;
                fileItem.Revision = doc.Rev;

                await UploadDocumentAttachment(fileItem);

                return fileItem;
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException($"Error: File not found.\nFile: {fileName}", fileName, ex);
            }   
            catch (IOException ex)
            {
                throw new IOException($"Error: Could not read the file.\nFile: {fileName}", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"Error: Could not read the file.\nFile: {fileName}", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"Error: Could not upload file.\nFile: {fileName}", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new OutOfMemoryException($"Error: Could not upload file bigger than 2GB.\nFile: {fileName}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: Some error occurred.\nFile: {fileName}", ex);
            }
        }

        private string RenameFileWithExistingName(string fileName, IEnumerable<string> existingFilesNames)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (existingFilesNames.Any(x => string.CompareOrdinal(x, nameWithoutExtension) == 0))
            {
                return $"{nameWithoutExtension} - {DateTime.Now.Ticks.ToString()}{Path.GetExtension(fileName)}";
            }

            return fileName;
        }

        private byte[] ReadBytes(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task UploadDocumentAttachment(FileItem fileItem)
        {
            var attachmentResult = await DataAccess.PutBinary(fileItem.Data, $"{fileItem.Id}/{fileItem.Name}", fileItem.Revision);

            if (attachmentResult.IsSuccessStatusCode)
            {
                var content = await attachmentResult.Content.ReadAsStringAsync();
                var rev = JObject.Parse(content)["rev"].ToString();
                fileItem.Revision = rev;
            }
            else
            {
                await ItemsDeleter.DeleteItem(fileItem);

                throw new Exception();
            }
        }
    }
}