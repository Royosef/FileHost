﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FileHost.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileHost.FilesManagement
{
    public class ItemsLoader
    {
        private DataAccess DataAccess { get; } = DataAccess.Instance;

        public async Task<List<FileItem>> GetFolderFiles(FolderItem folderItem)
        {
            var filesResult = await DataAccess.GetByView("docsByFolder", folderItem.Id, true);

            if (!filesResult.IsSuccessStatusCode)
            {
                throw new Exception($"Error: Could not sync {folderItem.Name}'s files.");
            }

            var filesJson = await filesResult.Content.ReadAsStringAsync();
            var filesItems = GetFolderFiles(filesJson);

            return filesItems;
        }

        private List<FileItem> GetFolderFiles(string filesJson)
        {
            var filesItems = new List<FileItem>();
            var jFilesItems = JArray.Parse(JObject.Parse(filesJson)["rows"].ToString()).ToList();

            foreach (var fileItemDoc in jFilesItems)
            {
                var fileItem = JsonConvert.DeserializeObject<FileItem>(fileItemDoc["doc"].ToString());
                filesItems.Add(fileItem);
            }

            return filesItems;
        }

        public async Task<List<FolderItem>> GetFolders()
        {
            var foldersResult = await DataAccess.GetByView("folders", includeDocs: true);

            if (!foldersResult.IsSuccessStatusCode)
            {
                throw new Exception("Error: Could not sync folders.");
            }

            var foldersJson = await foldersResult.Content.ReadAsStringAsync();
            var folderItems = await GetFolderItems(foldersJson);

            return folderItems;
        }

        private async Task<List<FolderItem>> GetFolderItems(string foldersJson)
        {
            var folderItems = new List<FolderItem>();
            var jFolderItems = JArray.Parse(JObject.Parse(foldersJson)["rows"].ToString()).ToList();

            foreach (var folderItemDoc in jFolderItems)
            {
                var folderItem = JsonConvert.DeserializeObject<FolderItem>(folderItemDoc["doc"].ToString());
                folderItem.ItemsAmount = await GetFolderItemsAmount(folderItem.Id);
                folderItems.Add(folderItem);
            }

            return folderItems;
        }

        private async Task<int> GetFolderItemsAmount(string folderId)
        {
            var amountResult  = await DataAccess.GetByView("amountOfFolderDocsByFolder", folderId);

            var jObjResult = JObject.Parse(await amountResult.Content.ReadAsStringAsync());

            if (!jObjResult["rows"].HasValues) return 0;

            var jObjAmount = jObjResult["rows"][0]["value"];

            return int.Parse(jObjAmount.ToString());
        }
    }
}