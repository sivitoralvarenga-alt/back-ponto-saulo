using System.ComponentModel;
using System.Runtime.CompilerServices;

public class RelogioViewModel : INotifyPropertyChanged
{
    private DateTime _horario;

    public DateTime Agora
    {
        get => _horario;
        set
        {
            _horario = value;
            OnPropertyChanged();
        }
    }

    public RelogioViewModel()
    {
        AtualizarHora();
        Application.Current?.Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            AtualizarHora();
            return true;
        });
    }

    private void AtualizarHora()
    {
        Agora = DateTime.Now;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] String? nome = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}