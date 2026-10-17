namespace AssurCompare.Web.Components;

[Flags]
public enum ProfileSections
{
    Lamal = 1,
    Situation = 2,
    Couvertures = 4,
    Tout = Lamal | Situation | Couvertures,
}
