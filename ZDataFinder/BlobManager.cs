using Azure.Data.Tables;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Identity.Client.Extensions.Msal;
using Orleans.Core;
using OrleansShardedStorageProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

namespace ZDataFinder
{
    internal class BlobManager
    {
        private List<TableClient> _tableClients = new List<TableClient>();
        private List<BlobContainerClient> _blobClients = new List<BlobContainerClient>();

        public BlobManager()
        {
        }

        public async Task Init(AzureShardedStorageOptions options)
        {
            foreach (var storage in options.ConnectionStrings)
            {
                if (storage.StorageType == StorageType.TableStorage)
                {
                    var shareClient = String.IsNullOrEmpty(storage.SasToken) ?
                        new TableServiceClient(storage.BaseTableUri, new DefaultAzureCredential()) :
                        new TableServiceClient(storage.BaseTableUri, new AzureSasCredential(storage.SasToken));

                    var table = await shareClient.CreateTableIfNotExistsAsync(storage.TableOrContainerName);


                    var tableClient = new TableClient(
                                storage.TableStorageUri,
                                new AzureSasCredential(storage.SasToken));

                    this._tableClients.Add(tableClient);
                }
                else if (storage.StorageType == StorageType.BlobStorage)
                {
                    BlobServiceClient blobServiceClient = (null == storage.SasCredential) ?
                        new BlobServiceClient(storage.BaseBlobUri, new DefaultAzureCredential()) :
                        new BlobServiceClient(storage.BaseBlobUri, storage.SasCredential);

                    var containerClient = blobServiceClient.GetBlobContainerClient(storage.TableOrContainerName);
                    await containerClient.CreateIfNotExistsAsync();

                    this._blobClients.Add(containerClient);
                }
                else
                {
                    throw new NotImplementedException("type not implmeneted");
                }
            }
        }


        public async Task<string> GetStorageAccountFromBlobKeyPart(string dataToFind)
        {
            string retVal = "";

            foreach (var blobClient in this._blobClients)
            {
                // Call the listing operation and return pages of the specified size.
                var resultSegment = blobClient.GetBlobsAsync()
                    .AsPages(default, 1000);

                // Enumerate the blobs returned for each page.
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        Console.WriteLine("Blob name: {0}", blobItem.Name);

                        if (blobItem.Name.ToLower().Contains(dataToFind))
                        {
                            retVal = blobClient.AccountName;
                            break;
                        }

                    }
                }

                if (!String.IsNullOrWhiteSpace(retVal))
                {
                    break;
                }

            }

            return retVal;
        }
    }
}
