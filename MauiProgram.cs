namespace Lab2Solution;

/**
Nmame : Woo Sik Choi
Date : 10/9/2022
Description : The third lab from software engineering class where we would have to connect it with bit.io database
Bugs :
Reflection :
 **/
public static class MauiProgram
{
    public static IBusinessLogic ibl = new BusinessLogic();

    public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		return builder.Build();
	}
}

