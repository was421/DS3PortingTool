using SoulsFormats;

namespace DS3PortingTool.Util;

public static class GXListExtensions
{
    public static bool Equals(this FLVER2.GXList list1, FLVER2.GXList list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            FLVER2.GXItem item1 = list1[i];
            FLVER2.GXItem item2 = list2[i];
            if (item1.ID != item2.ID ||
                item1.Unk04 != item2.Unk04 ||
                !item1.Data.SequenceEqual(item2.Data))
            {
                return false;
            }
        }
        
        return true;
    }
}