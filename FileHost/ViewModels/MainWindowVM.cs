using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FileHost.Annotations;
using FileHost.Infra;
using FileHost.Models;
using FileHost.Views;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace FileHost.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        public FolderItem CurrentFolder { get; set; } = null;
        public HttpClient Client { get; } = new HttpClient {BaseAddress = new Uri("http://127.0.0.1:5984/filehost/")};

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public DelegateCommand UploadFileCommand { get; }
        public DelegateCommand CreateFolderCommand { get; }

        public MainWindowVM()
        {
            UploadFileCommand = new DelegateCommand(Upload);
            CreateFolderCommand = new DelegateCommand(CreateFolder);
        }

        private async void CreateFolder()
        {
            var folderName = GetFolderName();
            if(folderName == null) return;

            try
            {
                var folderItem = new FolderItem
                {
                    Name = folderName,
                };

                var docResult = await Client.PostAsync(string.Empty, new StringContent(JsonConvert.SerializeObject(folderItem), Encoding.UTF8, "application/json"));

                if (!docResult.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error: Some error occurred.\nFolder: {folderItem.Name}");
                    return;
                }

                var doc = JsonConvert.DeserializeAnonymousType(await docResult.Content.ReadAsStringAsync(), new { Id = string.Empty, Rev = string.Empty });
                folderItem.Id = doc.Id;
                folderItem.Revision = doc.Rev;

                Items.Add(folderItem);
            }
            catch (Exception)
            {
                MessageBox.Show($"Error: Some error occurred, could not create folder.\nFolder: {folderName}");
            }
        }

        [CanBeNull]
        private string GetFolderName()
        {
            var dialog = new FolderNameWindow();
            var dialogResult = dialog.ShowDialog();

            if (dialogResult ?? false)
            {
                return (dialog.DataContext as FolderNameWindowVM)?.FolderName;
            }

            return  null;
        }

        private async void Upload()
        {
            var files = GetFiles();
            if (files == null) return;

            foreach (var file in files)
            {
                var fileItem = await CreateFileDocument(file);

                if (fileItem != null)
                {
                    Items.Add(fileItem);
                }
            }

            MessageBox.Show("Upload finished.");
        }

        [CanBeNull]
        private string[] GetFiles()
        {
            var fileDialog = new OpenFileDialog { Multiselect = true };
            var isFilesSelected = fileDialog.ShowDialog();

            return isFilesSelected != null && isFilesSelected == true ? fileDialog.FileNames : null;
        }

        private async Task<FileItem> CreateFileDocument(string file)
        {
            var fileName = Path.GetFileName(file);

            try
            {
                var bin = ReadBytes(file);

                var fileItem = new FileItem
                {
                    Data = bin,
                    Name = fileName,
                    ContainingFolderId = CurrentFolder?.Id
                };

                var docResult = await Client.PostAsync(string.Empty, new StringContent(JsonConvert.SerializeObject(fileItem), Encoding.UTF8, "application/json"));

                if (!docResult.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error: Some error occurred.\nFile: {fileItem.Name}");
                    return null;
                }

                var doc = JsonConvert.DeserializeAnonymousType(await docResult.Content.ReadAsStringAsync(), new {Id = string.Empty, Rev = string.Empty});
                fileItem.Id = doc.Id;
                fileItem.Revision = doc.Rev;

                if (!await UploadDocumentAttachment(fileItem)) return null;

                return fileItem;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Error: File not found.\nFile: {fileName}");
                return null;
            }
            catch (Exception ex) when(ex is IOException || ex is NotSupportedException)
            {
                MessageBox.Show($"Error: Could not read the file.\nFile: {fileName}");
                return null;
            }
            catch (HttpRequestException)
            {
                MessageBox.Show($"Error: Could not upload file.\nFile: {fileName}");
                return null;
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show($"Error: Could not upload file bigger than 2GB.\nFile: {fileName}");
                return null;
            }
            catch (Exception)
            {
                MessageBox.Show($"Error: Some error occurred.\nFile: {fileName}");
                return null;
            }
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

        private async Task<bool> UploadDocumentAttachment(FileItem fileItem)
        {
            var attachmentResult = await Client.PutAsync($"{fileItem.Id}/{fileItem.Name}?rev={fileItem.Revision}", new ByteArrayContent(fileItem.Data));

            if (attachmentResult.IsSuccessStatusCode) return attachmentResult.IsSuccessStatusCode;

            await DeleteDocument(fileItem);
            MessageBox.Show($"Error: Some error occurred.\nFile: {fileItem.Name}");

            return attachmentResult.IsSuccessStatusCode;
        }

        private async Task DeleteDocument(Item fileItem)
        {
            await Client.DeleteAsync($"{fileItem.Id}/{fileItem.Name}?rev={fileItem.Revision}");
        }
    }
}
