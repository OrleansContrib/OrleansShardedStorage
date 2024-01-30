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
using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using System.Xml;

namespace ZDataFinder
{
    internal class StorageDataFinder
    {
        private List<TableClient> _tableClients = new List<TableClient>();
        private List<BlobContainerClient> _blobClients = new List<BlobContainerClient>();

        public StorageDataFinder()
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


        public async Task<ConcurrentBag<string>> GetStorageAccountFromBlobKeyPart(string dataToFind, bool exitAtFirstResult)
        {
            ConcurrentBag<string> retVal = new ConcurrentBag<string>();

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 20
            };

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            try
            {
                await Parallel.ForEachAsync(this._blobClients, options, async (blobClient, ct) =>
                {
                    Console.WriteLine($"Checking {blobClient.AccountName}/{blobClient.Name}");

                    // Call the listing operation and return pages of the specified size.
                    var resultSegment = blobClient.GetBlobsAsync()
                        .AsPages(default, 1000);

                    // Enumerate the blobs returned for each page.
                    await foreach (Page<BlobItem> blobPage in resultSegment)
                    {
                        ct.ThrowIfCancellationRequested();

                        // If we have a result anywhere and we should bounce, bounce.
                        if (exitAtFirstResult && retVal.Any())
                        {
                            break;
                        }

                        foreach (BlobItem blobItem in blobPage.Values)
                        {
                            // TODO: Remove this!
                            Console.WriteLine("Blob name: {0}", blobItem.Name);

                            if (blobItem.Name.ToLower().Contains(dataToFind))
                            {
                                retVal.Add($"{blobClient.AccountName} / {blobItem.Name}");

                                if (exitAtFirstResult)
                                {
                                    Console.WriteLine("Found something. Exiting parallel foreach...");
                                    source.Cancel();
                                }

                                break;
                            }

                        }
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                // ... (the cts was cancelled)
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return retVal;
        }
    }
}
