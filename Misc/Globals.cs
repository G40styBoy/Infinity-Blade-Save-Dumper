using System.IO;
public class Globals
{
    //public const string saveFile = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Input\\*.bin"; 
    public static string[] saveFile = Directory.GetFiles("C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Input", "*.bin");
    public const string outputFile = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Deserialized Saves\\Deserialized.json"; 
    public const string binaryOutput = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Output\\UnencryptedSave0.bin"; 
    public const string someOuput = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\someOutput.txt"; 

    public const int nullTerm = 1;

    public const int packageBegin = 8;

    public const int InfoGrab = 4;


    public enum eTouchRewardActor
    {
        TRA_Random,                     // 0
        TRA_Random_Potion,              // 1
        TRA_Random_Gold,                // 2
        TRA_Random_Key,                 // 3
        TRA_Random_Gem,                 // 4
        TRA_Random_Item,                // 5
        TRA_Random_World,               // 6
        TRA_None,                       // 7
        TRA_Gold_Small,                 // 8
        TRA_Gold_Medium,                // 9
        TRA_Gold_Large,                 // 10
        TRA_Key_Small,                  // 11
        TRA_Key_Medium,                 // 12
        TRA_Key_Large,                  // 13
        TRA_Key_Item,                   // 14
        TRA_Gem_Fixed,                  // 15
        TRA_Item_Fixed,                 // 16
        TRA_Item_Weapon,                // 17
        TRA_Item_Shield,                // 18
        TRA_Item_Armor,                 // 19
        TRA_Item_Helmet,                // 20
        TRA_Item_Magic,                 // 21
        TRA_GrabBag_Small,              // 22
        TRA_GrabBag_Medium,             // 23
        TRA_GrabBag_Large,              // 24
        TRA_GrabBag_SmallGem,           // 25
        TRA_GrabBag_MediumGem,          // 26
        TRA_GrabBag_LargeGem,           // 27
        TRA_GrabBag_Uber,               // 28
        TRA_Potion_HealthL,             // 29
        TRA_Potion_Fixed,               // 30
        TRA_World_1_Cactus,             // 31
        TRA_World_2_Berries,            // 32
        TRA_World_3_PinkPlant,          // 33
        TRA_World_4_Reeds,              // 34
        TRA_World_5_DesertBulb,         // 35
        TRA_World_6_Mushroom,           // 36
        TRA_World_7_Root,               // 37
        TRA_World_8_Butterfly,          // 38
        TRA_World_9_Skullfly,           // 39
        TRA_World_10_Cocoon,            // 40
        TRA_World_11_Bones,             // 41
        TRA_World_12_Future,            // 42
        TRA_World_13_Future,            // 43
        TRA_World_14_Future,            // 44
        TRA_World_15_Future,            // 45
        TRA_World_16_Future,            // 46
        TRA_Chips_Small,                // 47
        TRA_Chips_Medium,               // 48
        TRA_Chips_Large,                // 49
        TRA_MAX                         // 50
    };
}