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
    public class IntKeyTests
    {
        IClusterClient _client;
        public IntKeyTests()
        {
            var host = OrleansSetup.StartClientAsync().Result;
            _client = host.Services.GetRequiredService<IClusterClient>();
        }

        [Theory]
        [Trait("Category", "Integer Key Tests")]
        [InlineData(0, "smalldata/0")]
        [InlineData(1, "smalldata/1")]
        [InlineData(2, "smalldata/2")]
        [InlineData(3, "smalldata/3")]
        [InlineData(4, "smalldata/4")]
        [InlineData(5, "smalldata/5")]
        [InlineData(6, "smalldata/6")]
        [InlineData(7, "smalldata/7")]
        [InlineData(8, "smalldata/8")]
        [InlineData(9, "smalldata/9")]
        [InlineData(10, "smalldata/A")]
        [InlineData(11, "smalldata/B")]
        [InlineData(12, "smalldata/C")]
        [InlineData(13, "smalldata/D")]
        [InlineData(14, "smalldata/E")]
        [InlineData(15, "smalldata/F")]
        [InlineData(16, "smalldata/10")]
        [InlineData(17, "smalldata/11")]
        [InlineData(18, "smalldata/12")]
        [InlineData(19, "smalldata/13")]
        [InlineData(20, "smalldata/14")]
        [InlineData(21, "smalldata/15")]
        [InlineData(22, "smalldata/16")]
        [InlineData(23, "smalldata/17")]
        [InlineData(24, "smalldata/18")]
        [InlineData(25, "smalldata/19")]
        [InlineData(26, "smalldata/1A")]
        [InlineData(27, "smalldata/1B")]
        [InlineData(28, "smalldata/1C")]
        [InlineData(29, "smalldata/1D")]
        public async void SmallDataGrains_WithIntKeys_ExpectGrainIdsToMatch(
            int grainIdToInstantiate,
            string expectedGrainId
            )
        {
            // Arrange
   

            // Act
            var grain = _client.GetGrain<ISmallDataGrain>(grainIdToInstantiate);
            GrainId grainId = grain.GetGrainId();
            var actualGrainId = grainId.ToString();

            // Assert
            Assert.Equal(expectedGrainId, actualGrainId);
        }
    }
}
