namespace VitalCares.Views;

public partial class DashboardPage : ContentPage
{
	public DashboardPage(ViewModels.MainViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}