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
        [InlineData("418eccf6-3ffa-4e03-8b86-989b8a9564a2", "smalldatawithguidkey/418eccf63ffa4e038b86989b8a9564a2")]
        //[InlineData("080563f5-bb61-413f-8737-16af064705aa", "smalldatawithguidkey/080563f5bb61413f873716af064705aa")]
        //[InlineData("08765ce0-4254-4915-b26e-f93859ac4cac", "smalldatawithguidkey/08765ce042544915b26ef93859ac4cac")]
        //[InlineData("5757f716-fa3d-4119-9dda-ffe1775e6e9b", "smalldatawithguidkey/5757f716fa3d41199ddaffe1775e6e9b")]
        //[InlineData("5a475386-0fc6-431a-9937-52ed777d65b9", "smalldatawithguidkey/5a4753860fc6431a993752ed777d65b9")]
        //[InlineData("55830085-e13c-4032-9721-f8bf27f7a255", "smalldatawithguidkey/55830085e13c40329721f8bf27f7a255")]
        //[InlineData("5a2f83db-13a5-4c03-8a86-6dc0d63aff96", "smalldatawithguidkey/5a2f83db13a54c038a866dc0d63aff96")]
        //[InlineData("efe6b946-f7c0-4ead-ae78-532f6bd21612", "smalldatawithguidkey/efe6b946f7c04eadae78532f6bd21612")]
        //[InlineData("490d018a-1ebc-435e-a046-99537651dd24", "smalldatawithguidkey/490d018a1ebc435ea04699537651dd24")]
        //[InlineData("29706b5f-be6c-474d-b7ee-d0da9785eea0", "smalldatawithguidkey/29706b5fbe6c474db7eed0da9785eea0")]
        //[InlineData("7227cf04-6886-4ec0-9936-83a2fc4a26ed", "smalldatawithguidkey/7227cf0468864ec0993683a2fc4a26ed")]
        //[InlineData("e7e2b5ed-18c5-4e32-a3ec-701a29d4d87f", "smalldatawithguidkey/e7e2b5ed18c54e32a3ec701a29d4d87f")]
        //[InlineData("b1adbde2-6128-4f9c-ba63-c16b4127cb0e", "smalldatawithguidkey/b1adbde261284f9cba63c16b4127cb0e")]
        //[InlineData("fab0a861-bc6a-40d3-be54-945e57032c70", "smalldatawithguidkey/fab0a861bc6a40d3be54945e57032c70")]
        //[InlineData("e212f313-70ae-4150-ab12-a90e19b845ad", "smalldatawithguidkey/e212f31370ae4150ab12a90e19b845ad")]
        //[InlineData("8cb5cf94-13f7-49fc-b565-812a7075e45d", "smalldatawithguidkey/8cb5cf9413f749fcb565812a7075e45d")]
        //[InlineData("fd634b5e-05b4-4f71-b6e9-b61f965430b3", "smalldatawithguidkey/fd634b5e05b44f71b6e9b61f965430b3")]
        //[InlineData("898aa0ab-fee5-4b4e-82f8-e86b49fe8ad1", "smalldatawithguidkey/898aa0abfee54b4e82f8e86b49fe8ad1")]
        //[InlineData("f2904a47-6e7f-4e91-abc8-4684eec41172", "smalldatawithguidkey/f2904a476e7f4e91abc84684eec41172")]
        //[InlineData("aab55616-c731-4281-80fb-7cf704a30d9a", "smalldatawithguidkey/aab55616c731428180fb7cf704a30d9a")]
        //[InlineData("170d9e08-d07c-4120-a4eb-da612b1466ed", "smalldatawithguidkey/170d9e08d07c4120a4ebda612b1466ed")]
        //[InlineData("79d295d6-b9b1-45fb-ac62-38589d8fda89", "smalldatawithguidkey/79d295d6b9b145fbac6238589d8fda89")]
        //[InlineData("98d1bfe4-0c72-438b-a637-61cd1923e02b", "smalldatawithguidkey/98d1bfe40c72438ba63761cd1923e02b")]
        //[InlineData("047cc294-8672-4e5d-9177-d56dcf98ef23", "smalldatawithguidkey/047cc29486724e5d9177d56dcf98ef23")]
        //[InlineData("56f8d1f5-3198-4325-b39b-10c132d5efe2", "smalldatawithguidkey/56f8d1f531984325b39b10c132d5efe2")]
        //[InlineData("e43f4b20-c6f0-4200-8bdf-03eb7ed15987", "smalldatawithguidkey/e43f4b20c6f042008bdf03eb7ed15987")]
        //[InlineData("752ee328-d93a-4ec6-904a-756772e999a3", "smalldatawithguidkey/752ee328d93a4ec6904a756772e999a3")]
        //[InlineData("64b09c53-50e5-4213-b23e-bdd62d6ec0b9", "smalldatawithguidkey/64b09c5350e54213b23ebdd62d6ec0b9")]
        //[InlineData("c1dc1d33-8aa5-4413-af3d-cb4f937f922a", "smalldatawithguidkey/c1dc1d338aa54413af3dcb4f937f922a")]
        //[InlineData("c8a32e90-6d77-4ee3-bbf9-4f1d23b32b27", "smalldatawithguidkey/c8a32e906d774ee3bbf94f1d23b32b27")]

        public async void SmallDataWithGuidKeyGrains_WithGuidKeys_ExpectKeysToMatch(
            string grainIdToInstantiate,
            string expectedGrainId
            )
        {
            // Arrange
            var host = await OrleansSetup.StartClientAsync();
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
