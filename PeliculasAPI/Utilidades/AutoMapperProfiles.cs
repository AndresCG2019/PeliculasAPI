using AutoMapper;
using NetTopologySuite.Geometries;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles(GeometryFactory geometryFactory) 
        {
            // gavilanch utiliza la version 8.1.0 del nugget
            
            CreateMap<Genero, GeneroDTO>().ReverseMap();
            CreateMap<GeneroCreacionDTO, Genero>();

            CreateMap<Actor, ActorDTO>().ReverseMap();
            CreateMap<ActorCreacionDTO, Actor>().ForMember(x => x.Foto, options => options.Ignore());

            // HACER LOS CINES A MANO POR FALLO EN TIPO DE DATO POINT

            CreateMap<PeliculaCreacionDTO, Pelicula>()
                .ForMember(x => x.Poster, opciones => opciones.Ignore())
                .ForMember(x => x.PeliculasGeneros, opciones => opciones.MapFrom(MapearPeliculasGeneros))
                .ForMember(x => x.PeliculasCines, opciones => opciones.MapFrom(MapearPeliculasCines))
                .ForMember(x => x.PeliculasActores, opciones => opciones.MapFrom(MapearPeliculasActores));
        }

        private List<PeliculasGeneros> MapearPeliculasGeneros(PeliculaCreacionDTO peliculaCreacionDTO, Pelicula pelicula) 
        {
            var resultado = new List<PeliculasGeneros>();
            if (peliculaCreacionDTO.GenerosIds == null)
            {
                return resultado;
            }

            foreach (var id in peliculaCreacionDTO.GenerosIds)
            {
                resultado.Add(new PeliculasGeneros() { GeneroId = id });
            }
            return resultado;
        }

        private List<PeliculasCines> MapearPeliculasCines(PeliculaCreacionDTO peliculaCreacionDTO, Pelicula pelicula)
        {
            var resultado = new List<PeliculasCines>();
            if (peliculaCreacionDTO.GenerosIds == null)
            {
                return resultado;
            }

            foreach (var id in peliculaCreacionDTO.CinesIds)
            {
                resultado.Add(new PeliculasCines() { CineId = id });
            }
            return resultado;
        }

        private List<PeliculasActores> MapearPeliculasActores(PeliculaCreacionDTO peliculaCreacionDTO, Pelicula pelicula)
        {
            var resultado = new List<PeliculasActores>();
            if (peliculaCreacionDTO.GenerosIds == null)
            {
                return resultado;
            }

            foreach (var actor in peliculaCreacionDTO.Actores)
            {
                resultado.Add(new PeliculasActores() { ActorId = actor.Id, Personaje = actor.Personaje });
            }
            return resultado;
        }
    }
}
