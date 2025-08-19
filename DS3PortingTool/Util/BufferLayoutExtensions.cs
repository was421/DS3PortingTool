using SoulsFormats;

namespace DS3PortingTool.Util;

public static class BufferLayoutExtensions
{
    public static bool Equals(this FLVER2.BufferLayout layout, FLVER2.BufferLayout other)
    {
        if (layout.Count != other.Count) return false;
        for (int i = 0; i < layout.Count; i++)
        {
            FLVER.LayoutMember memberA = layout[i];
            FLVER.LayoutMember memberB = other[i];

            if (memberA.Type != memberB.Type ||
                memberA.Semantic != memberB.Semantic ||
                memberA.Index != memberB.Index ||
                memberA.Stream != memberB.Stream ||
                memberA.SpecialModifier != memberB.SpecialModifier)
            {
                return false;
            }
        }
        
        return true;
    }
}