using Monetco.Host.Domain;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monetco
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SimulatorRequest, SimulatorResponse>()
                .ForMember(s => s.Content, o => o.MapFrom(d => d.Value))
                .ForMember(s => s.StatusCode, o => o.MapFrom(d => d.StatusCode != 0 ? d.StatusCode : 200))
                .ForMember(s => s.Regexp, o => o.MapFrom(d => d.Regexp))
                .ForMember(s => s.Url, o => o.MapFrom(d => d.Url))
                .ForMember(s => s.ContentType, o =>
                {
                    o.Condition(s => !string.IsNullOrEmpty(s.ContentType));
                    o.MapFrom(s => s.ContentType);
                })
                .ForMember(s => s.Scope, o => o.MapFrom(d => d.GetScope()));

            CreateMap<SimulatorResponse, SimulatorRequest>()
                .ForMember(s => s.Value, o => o.MapFrom(d => d.Content))
                .ForMember(s => s.StatusCode, o => o.MapFrom(d => d.StatusCode != 0 ? d.StatusCode : 200))
                .ForMember(s => s.Regexp, o => o.MapFrom(d => d.Regexp))
                .ForMember(s => s.Url, o => o.MapFrom(d => d.Url))
                .ForMember(s => s.ContentType, o =>
                {
                    o.Condition(s => !string.IsNullOrEmpty(s.ContentType));
                    o.MapFrom(s => s.ContentType);
                });
        }
    }
}
