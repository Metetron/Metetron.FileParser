using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AutoMapper;
using Metetron.FileParser.FileTasks;
using Metetron.FileParser.Options;
using Metetron.FileParser.WatcherConfiguration;

namespace Metetron.FileParser
{
    public class FileWatcherProfile : Profile
    {
        public FileWatcherProfile(IFileSystem fileSystem)
        {
            CreateMap<WatcherOptions, FileCheckOptions>()
                .ForMember(co => co.FileSearchPattern, opt => opt.MapFrom(fo => new Regex(fo.FileSearchPattern)))
                .ForMember(co => co.SubDirectorySearchPattern, opt => opt.MapFrom(fo => new Regex(fo.SubDirectorySearchPattern)))
                .ForMember(co => co.MainDirectory, opt => opt.MapFrom(fo => fileSystem.DirectoryInfo.FromDirectoryName(fo.DirectoryToWatch)));
        }
    }
}