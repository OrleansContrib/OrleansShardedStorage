using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
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
        [Theory]
        [Trait("Category", "Role Tests")]
        [InlineData("418eccf6-3ffa-4e03-8b86-989b8a9564a2", "largedata/418eccf6-3ffa-4e03-8b86-989b8a9564a2")]
        //[InlineData("080563f5-bb61-413f-8737-16af064705aa", "largedata/080563f5-bb61-413f-8737-16af064705aa")]
        //[InlineData("08765ce0-4254-4915-b26e-f93859ac4cac", "largedata/08765ce0-4254-4915-b26e-f93859ac4cac")]
        //[InlineData("5757f716-fa3d-4119-9dda-ffe1775e6e9b", "largedata/5757f716-fa3d-4119-9dda-ffe1775e6e9b")]
        //[InlineData("5a475386-0fc6-431a-9937-52ed777d65b9", "largedata/5a475386-0fc6-431a-9937-52ed777d65b9")]
        //[InlineData("55830085-e13c-4032-9721-f8bf27f7a255", "largedata/55830085-e13c-4032-9721-f8bf27f7a255")]
        //[InlineData("5a2f83db-13a5-4c03-8a86-6dc0d63aff96", "largedata/5a2f83db-13a5-4c03-8a86-6dc0d63aff96")]
        //[InlineData("efe6b946-f7c0-4ead-ae78-532f6bd21612", "largedata/efe6b946-f7c0-4ead-ae78-532f6bd21612")]
        //[InlineData("490d018a-1ebc-435e-a046-99537651dd24", "largedata/490d018a-1ebc-435e-a046-99537651dd24")]
        //[InlineData("29706b5f-be6c-474d-b7ee-d0da9785eea0", "largedata/29706b5f-be6c-474d-b7ee-d0da9785eea0")]
        //[InlineData("7227cf04-6886-4ec0-9936-83a2fc4a26ed", "largedata/7227cf04-6886-4ec0-9936-83a2fc4a26ed")]
        //[InlineData("e7e2b5ed-18c5-4e32-a3ec-701a29d4d87f", "largedata/e7e2b5ed-18c5-4e32-a3ec-701a29d4d87f")]
        //[InlineData("b1adbde2-6128-4f9c-ba63-c16b4127cb0e", "largedata/b1adbde2-6128-4f9c-ba63-c16b4127cb0e")]
        //[InlineData("fab0a861-bc6a-40d3-be54-945e57032c70", "largedata/fab0a861-bc6a-40d3-be54-945e57032c70")]
        //[InlineData("e212f313-70ae-4150-ab12-a90e19b845ad", "largedata/e212f313-70ae-4150-ab12-a90e19b845ad")]
        //[InlineData("8cb5cf94-13f7-49fc-b565-812a7075e45d", "largedata/8cb5cf94-13f7-49fc-b565-812a7075e45d")]
        //[InlineData("fd634b5e-05b4-4f71-b6e9-b61f965430b3", "largedata/fd634b5e-05b4-4f71-b6e9-b61f965430b3")]
        //[InlineData("898aa0ab-fee5-4b4e-82f8-e86b49fe8ad1", "largedata/898aa0ab-fee5-4b4e-82f8-e86b49fe8ad1")]
        //[InlineData("f2904a47-6e7f-4e91-abc8-4684eec41172", "largedata/f2904a47-6e7f-4e91-abc8-4684eec41172")]
        //[InlineData("aab55616-c731-4281-80fb-7cf704a30d9a", "largedata/aab55616-c731-4281-80fb-7cf704a30d9a")]
        //[InlineData("170d9e08-d07c-4120-a4eb-da612b1466ed", "largedata/170d9e08-d07c-4120-a4eb-da612b1466ed")]
        //[InlineData("79d295d6-b9b1-45fb-ac62-38589d8fda89", "largedata/79d295d6-b9b1-45fb-ac62-38589d8fda89")]
        //[InlineData("98d1bfe4-0c72-438b-a637-61cd1923e02b", "largedata/98d1bfe4-0c72-438b-a637-61cd1923e02b")]
        //[InlineData("047cc294-8672-4e5d-9177-d56dcf98ef23", "largedata/047cc294-8672-4e5d-9177-d56dcf98ef23")]
        //[InlineData("56f8d1f5-3198-4325-b39b-10c132d5efe2", "largedata/56f8d1f5-3198-4325-b39b-10c132d5efe2")]
        //[InlineData("e43f4b20-c6f0-4200-8bdf-03eb7ed15987", "largedata/e43f4b20-c6f0-4200-8bdf-03eb7ed15987")]
        //[InlineData("752ee328-d93a-4ec6-904a-756772e999a3", "largedata/752ee328-d93a-4ec6-904a-756772e999a3")]
        //[InlineData("64b09c53-50e5-4213-b23e-bdd62d6ec0b9", "largedata/64b09c53-50e5-4213-b23e-bdd62d6ec0b9")]
        //[InlineData("c1dc1d33-8aa5-4413-af3d-cb4f937f922a", "largedata/c1dc1d33-8aa5-4413-af3d-cb4f937f922a")]
        //[InlineData("c8a32e90-6d77-4ee3-bbf9-4f1d23b32b27", "largedata/c8a32e90-6d77-4ee3-bbf9-4f1d23b32b27")]

        public async void TestLargeDataGrains_WithGuidKeys_ExpectKeysToMatch(
            string grainIdToInstantiate,
            string expectedGrainId
            )
        {
            // Arrange
            var host = await OrleansClient.StartClientAsync();
            var client = host.Services.GetRequiredService<IClusterClient>();
            var grainGuid = new Guid(grainIdToInstantiate);


            // Act
            var grain = client.GetGrain<ISmallDataWithGuidKeyGrain>(grainGuid);
            var actualGrainId = grain.GetGrainId().ToString();

            // Assert
            Assert.Equal(expectedGrainId, actualGrainId);
        }
    }
}
