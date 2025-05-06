using Rcb.MessageReader.Service;
using Rcb.MessageReader.Service.Repositories.Contracts;
using Rcb.MessageReader.Service.Repositories.Implementations;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IChannelRepository, ChannelRepository>();

var host = builder.Build();
host.Run();