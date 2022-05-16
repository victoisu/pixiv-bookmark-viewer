using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PixivBookmarkViewer;
using PixivBookmarkViewer.Services;
using PixivBookmarkViewer.Background;


var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
	ContentRootPath = Path.Combine("Website")
});

/////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddWebOptimizer(pipeline =>
{
	pipeline.MinifyCssFiles("lib/**/*.css");
	pipeline.MinifyJsFiles("lib/**/*.js");
});

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<FileService>();

builder.Services.AddSingleton<PixivQueueService>();
builder.Services.AddHostedService(service => service.GetService<PixivQueueService>());

builder.Services.AddSingleton<BacklogService>();
builder.Services.AddHostedService(service => service.GetService<BacklogService>());

builder.Services.AddTransient<PixivService>();
builder.Services.AddTransient<PixivApiService>();
builder.Services.AddTransient<ThumbnailService>();
builder.Services.AddHostedService<ArchiverService>();

/////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseWebOptimizer();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
	endpoints.MapRazorPages();
	endpoints.MapControllers();
});

app.Run();