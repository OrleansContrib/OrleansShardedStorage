using Xunit;

namespace UnitTests
{
    public class XSimpleTest
    {
        [Fact]
        public async void Test_CanRunAUnitTest_Expect_Success()
        {
            // Arrange - done in InlineData

            // Act

            // Assert
            Assert.Equal(1, 1);
        }

    }
}