using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class GuidKeyTests
    {
        IClusterClient _client;
        public GuidKeyTests()
        {
            var host = OrleansSetup.StartClientAsync().Result;
            _client = host.Services.GetRequiredService<IClusterClient>();
        }

        [Theory]
        [Trait("Category", "Guid Tests")]
        [InlineData("418eccf6-3ffa-4e03-8b86-989b8a9564a2", "smalldatawithguidkey/418eccf63ffa4e038b86989b8a9564a2")]
        [InlineData("080563f5-bb61-413f-8737-16af064705aa", "smalldatawithguidkey/080563f5bb61413f873716af064705aa")]
        [InlineData("08765ce0-4254-4915-b26e-f93859ac4cac", "smalldatawithguidkey/08765ce042544915b26ef93859ac4cac")]
        [InlineData("5757f716-fa3d-4119-9dda-ffe1775e6e9b", "smalldatawithguidkey/5757f716fa3d41199ddaffe1775e6e9b")]
        [InlineData("5a475386-0fc6-431a-9937-52ed777d65b9", "smalldatawithguidkey/5a4753860fc6431a993752ed777d65b9")]
        [InlineData("55830085-e13c-4032-9721-f8bf27f7a255", "smalldatawithguidkey/55830085e13c40329721f8bf27f7a255")]
        [InlineData("5a2f83db-13a5-4c03-8a86-6dc0d63aff96", "smalldatawithguidkey/5a2f83db13a54c038a866dc0d63aff96")]
        [InlineData("efe6b946-f7c0-4ead-ae78-532f6bd21612", "smalldatawithguidkey/efe6b946f7c04eadae78532f6bd21612")]
        [InlineData("490d018a-1ebc-435e-a046-99537651dd24", "smalldatawithguidkey/490d018a1ebc435ea04699537651dd24")]
        [InlineData("29706b5f-be6c-474d-b7ee-d0da9785eea0", "smalldatawithguidkey/29706b5fbe6c474db7eed0da9785eea0")]
        public async void SmallDataWithGuidKeyGrains_WithGuidKeys_ExpectGrainIdsToMatch(
            string grainIdToInstantiate,
            string expectedGrainId
            )
        {
            // Arrange
            var grainGuid = new Guid(grainIdToInstantiate);


            // Act
            var grain = _client.GetGrain<ISmallDataWithGuidKeyGrain>(grainGuid);
            GrainId grainId = grain.GetGrainId();
            var actualGrainId = grainId.ToString();

            // Assert
            Assert.Equal(expectedGrainId, actualGrainId);
        }
    }
}
