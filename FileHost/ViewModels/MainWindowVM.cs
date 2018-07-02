using System;
using System.IO;
using System.Net.Http;
using System.Text;
using FileHost.Infra;
using Microsoft.Win32;

namespace FileHost.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        public DelegateCommand OpenFileDialogCommand { get; } = new DelegateCommand(OpenFileDialog);

        private static async void OpenFileDialog()
        {
            var fileDialog = new OpenFileDialog {Multiselect = true};
            var isFilesSelected = fileDialog.ShowDialog();

            if (isFilesSelected == null || isFilesSelected != true) return;

            using (var client = new HttpClient {BaseAddress = new Uri("http://127.0.0.1:5984/filehost")})
            {
                using (var stream =
                    new StreamReader(new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read)))
                {
                    var bin = stream.ReadToEnd();

                    var result = await client.PostAsync(string.Empty,
                        new StringContent(
                            $"{{\"name\": \"{fileDialog.SafeFileName}\", \"_attachments\":\"{{\"{fileDialog.SafeFileName}\":\"{{\"data\":\"{bin}\", \"content_type\":\"application/x-binary\"}}\"}}\"}}",
                            Encoding.UTF8, "application/json"));
                }
            }
        }
    }
}
