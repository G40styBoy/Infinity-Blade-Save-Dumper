using SaveDumper.UnrealPackageManager;
using Xunit;

namespace Tests;

public class Deserialization_TEST
{

    [Theory]
    [MemberData(nameof(Global.SaveList), MemberType = typeof(Global))]
    public void TestMultipleFiles(string filePath)
    {
        using (var UPK = new UnrealPackage(filePath, PackageType.IB3))
        {
            var properties = UPK.DeserializeUPK();

            Assert.NotNull(properties);
        }
    }

    // [Theory]
    // [InlineData()]
    // public void TestSingleFile()
    // {
        
    // }   
}
