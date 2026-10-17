namespace AssurCompare.Web.Services;

/// <summary>Préférences d'affichage lues dans le navigateur (mouvement réduit, écran mobile).</summary>
public sealed class UiState
{
    public bool ReducedMotion { get; set; }
    public bool IsMobile { get; set; }
}

public sealed record UiInit(bool Mobile, bool ReducedMotion);
