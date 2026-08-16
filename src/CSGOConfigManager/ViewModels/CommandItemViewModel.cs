using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.ViewModels;

public sealed class CommandItemViewModel : ViewModelBase
{
    private string _currentValue;
    private string _originalValue;
    private string? _validationError;
    private bool _isModified;

    public CommandDefinition Definition { get; }

    public string Name => Definition.Name;
    public string Category => Definition.Category;
    public string Description => Definition.Description;
    public string Type => Definition.Type;
    public string DefaultValue => Definition.DefaultAsString();
    public string File => Definition.File;
    public bool RequiresRestart => Definition.RequiresRestart;
    public bool RequiresSvCheats => Definition.RequiresSvCheats;
    public double? Min => Definition.Min;
    public double? Max => Definition.Max;
    public IReadOnlyList<string> EnumValues => Definition.EnumValues ?? (IReadOnlyList<string>)Array.Empty<string>();
    public string ModesDisplay => string.Join(", ", Definition.Modes);

    public string CurrentValue
    {
        get => _currentValue;
        set
        {
            if (_currentValue == value)
                return;

            var validation = CommandValidator.Validate(Definition, value);
            if (!validation.IsValid)
            {
                _validationError = validation.ErrorMessage;
                OnPropertyChanged(nameof(ValidationError));
                OnPropertyChanged(nameof(HasValidationError));
                SetProperty(ref _currentValue, value);
                IsModified = true;
                return;
            }

            SetProperty(ref _currentValue, validation.NormalizedValue ?? value);
            _validationError = null;
            OnPropertyChanged(nameof(ValidationError));
            OnPropertyChanged(nameof(HasValidationError));
            IsModified = !string.Equals(_currentValue, _originalValue, StringComparison.Ordinal);
            OnPropertyChanged(nameof(IsDifferentFromDefault));
            OnPropertyChanged(nameof(BoolValue));
            OnPropertyChanged(nameof(NumericValue));
        }
    }

    public bool BoolValue
    {
        get => CurrentValue is "1" or "true" or "True";
        set => CurrentValue = value ? "1" : "0";
    }

    public double NumericValue
    {
        get => double.TryParse(CurrentValue, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : Definition.Min ?? 0;
        set => CurrentValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool IsModified
    {
        get => _isModified;
        private set => SetProperty(ref _isModified, value);
    }

    public bool IsDifferentFromDefault =>
        !string.Equals(CurrentValue, DefaultValue, StringComparison.OrdinalIgnoreCase);

    public string? ValidationError => _validationError;
    public bool HasValidationError => !string.IsNullOrEmpty(_validationError);

    public string FlagsDisplay
    {
        get
        {
            var flags = new List<string>();
            if (RequiresRestart) flags.Add("Restart");
            if (RequiresSvCheats) flags.Add("sv_cheats");
            return flags.Count == 0 ? "—" : string.Join(", ", flags);
        }
    }

    public CommandItemViewModel(CommandDefinition definition, string? currentValue)
    {
        Definition = definition;
        _currentValue = currentValue ?? definition.DefaultAsString();
        _originalValue = _currentValue;
    }

    public void ResetToDefault() => CurrentValue = DefaultValue;

    public void MarkSaved()
    {
        _originalValue = _currentValue;
        IsModified = false;
    }
}
