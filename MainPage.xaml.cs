using PontoSaulo.Data;

namespace PontoSaulo;

public partial class MainPage : ContentPage
{
	public MainPage(DatabaseService database)
	{
		InitializeComponent();
		PathLabel.Text = database.DatabasePath;
	}
}
