#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using WB.Core.SharedKernels.DataCollection;
using WB.Infrastructure.Native.Workspaces;

namespace WB.Core.BoundedContexts.Headquarters.Storage.AmazonS3
{
    public class AmazonS3Configuration : IAmazonS3Configuration
    {
        private readonly IOptions<FileStorageConfig> fileStorageOptions;
        private readonly IOptions<HeadquartersConfig> hqOptions;

        public AmazonS3Configuration(
            IOptions<FileStorageConfig> fileStorageOptions,
            IOptions<HeadquartersConfig> hqOptions)
        {
            this.fileStorageOptions = fileStorageOptions;
            this.hqOptions = hqOptions;
        }

        private AmazonBucketInfo? bucketInfo;

        [SuppressMessage("ReSharper", "CommentTypo")]
        public AmazonBucketInfo GetAmazonS3BucketInfo(WorkspaceContext? workspaceContext = null)
        {
            if (bucketInfo != null) return bucketInfo;

            var s3Url = fileStorageOptions.Value.AppData;

            var uri = new Uri(s3Url); // example: new Uri("s3://deccapi/hq/")

            var tenantName = this.hqOptions.Value.TenantName;
            
            bucketInfo = new AmazonBucketInfo(
                uri.Host, // example => deccapi
                uri.AbsolutePath.Trim('/') + "/" + tenantName + "/" 
                // example => hq/tenantName/
            );
            
            return bucketInfo;
        }
    }
}
