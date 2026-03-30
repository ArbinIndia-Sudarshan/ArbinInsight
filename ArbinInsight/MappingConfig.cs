using ArbinInsight.Models;
using ArbinInsight.Models.Dto;
using AutoMapper;

namespace ArbinInsight
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<Test, TestDto>().ReverseMap();
                config.CreateMap<TestProfile, TestProfileDto>().ReverseMap();

            });
            return mappingConfig;
        }
    }
}
