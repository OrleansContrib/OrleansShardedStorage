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
    public class StringKeyTests
    {
        IClusterClient _client;
        public StringKeyTests()
        {
            var host = OrleansSetup.StartClientAsync().Result;
            _client = host.Services.GetRequiredService<IClusterClient>();
        }

        [Theory]
        [Trait("Category", "String Key Tests")]
        [InlineData("bBIVZS1jpTou7Wqsdfhgbfds", "largedata/bBIVZS1jpTou7Wqsdfhgbfds")]
        [InlineData("cJ0yDVot74jnYIu", "largedata/cJ0yDVot74jnYIu")]
        [InlineData("zWdvQZe6WXesWZk", "largedata/zWdvQZe6WXesWZk")]
        [InlineData("klNxQmXw2nKVhkd", "largedata/klNxQmXw2nKVhkd")]
        [InlineData("ubQqHByZnof", "largedata/ubQqHByZnof")]
        [InlineData("cIqxoet6FTxeIYy", "largedata/cIqxoet6FTxeIYy")]
        [InlineData("2G7FH8YWfL0fP7S", "largedata/2G7FH8YWfL0fP7S")]
        [InlineData("zw88L", "largedata/zw88L")]
        [InlineData("JAJ7fBrCdCF9zzrsadfgjhmnbfdsaqw345SDFG", "largedata/JAJ7fBrCdCF9zzrsadfgjhmnbfdsaqw345SDFG")]
        [InlineData("zv9lJ6L5BhniS6s", "largedata/zv9lJ6L5BhniS6s")]
        public async void LargeDataGrains_WithStringKeys_ExpectGrainIdsToMatch(
            string grainIdToInstantiate,
            string expectedGrainId
            )
        {
            // Arrange


            // Act
            var grain = _client.GetGrain<ILargeDataGrain>(grainIdToInstantiate);
            GrainId grainId = grain.GetGrainId();
            var actualGrainId = grainId.ToString();

            // Assert
            Assert.Equal(expectedGrainId, actualGrainId);
        }
    }
}

