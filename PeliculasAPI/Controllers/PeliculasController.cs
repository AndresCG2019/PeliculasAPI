using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Utilidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    public class PeliculasController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;

        public PeliculasController(ApplicationDbContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos) 
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
        }

        [HttpGet]
        public async Task<ActionResult<LandingPageDTO>> Get()
        {
            var top = 6;
            var hoy = DateTime.Today;

            var proximosEstrenos = await context.Peliculas
                .Where(x => x.FechaLanzamiento > hoy)
                .OrderBy(x => x.FechaLanzamiento)
                .Take(top)
                .ToListAsync();

            var enCines = await context.Peliculas
                .Where(x => x.EnCines)
                .OrderBy(x => x.FechaLanzamiento)
                .Take(top)
                .ToListAsync();

            var resultado = new LandingPageDTO();
            resultado.ProximosEstrenos = mapper.Map<List<PeliculaDTO>>(proximosEstrenos);
            resultado.EnCines = mapper.Map<List<PeliculaDTO>>(enCines);

            return resultado;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PeliculaDTO>> Get(int Id) 
        {
            try
            {
                Pelicula pelicula = await context.Peliculas
                        .Include(x => x.PeliculasGeneros).ThenInclude(x => x.Genero)
                        .Include(x => x.PeliculasActores).ThenInclude(x => x.Actor)
                        .Include(x => x.PeliculasCines).ThenInclude(x => x.Cine)
                        .FirstOrDefaultAsync(x => x.Id == Id);

                if (pelicula == null)
                {
                    return NotFound();
                }

                PeliculaDTO dto = mapper.Map<PeliculaDTO>(pelicula); // a dto despues de esto le faltan actores y cines

                List<ActorDTO> actores = new List<ActorDTO>();
                
                // se le agregan actores al dto manualmente por fallo en mapper
                foreach (var item in pelicula.PeliculasActores)
                {
                    actores.Add(new ActorDTO() 
                    {
                        Id = item.ActorId,
                        Nombre = item.Actor.Nombre,
                        Biografia = item.Actor.Biografia,
                        FechaNacimiento = item.Actor.FechaNacimiento,
                        Foto = item.Actor.Foto
                    });
                }

                dto.Actores = actores;

                List<CineDTO> cines = new List<CineDTO>();

                // se le agregan cines al dto manualmente por fallo en mapper
                foreach (var item in pelicula.PeliculasCines)
                {
                    cines.Add(new CineDTO()
                    {
                        Id = item.CineId,
                        Nombre = item.Cine.Nombre,
                    });
                }

                dto.Cines = cines;

                return dto;
            }
            catch (Exception ex1)
            {

                throw;
            }    
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] PeliculaCreacionDTO peliculaCreacionDTO) // SI SE ARMA. SIGUE CON EL CURSO
        {
            Pelicula pelicula = mapper.Map<Pelicula>(peliculaCreacionDTO);
            pelicula.FechaLanzamiento = peliculaCreacionDTO.FechaEstreno;

            EscribirOrdenActores(pelicula);

            context.Add(pelicula);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("PostGet")]
        public async Task<ActionResult<PeliculasPostGetDTO>> PostGet() 
        {
            List<Cine> cines = await context.Cines.ToListAsync();
            List<Genero> generos = await context.Generos.ToListAsync();

            // mapeo a cinesdto a mano por fallo en tipo de dato point

            List<CineDTO> cinesDTO = new List<CineDTO>();

            foreach (var item in cines)
            {
                CineDTO cineDTO = new CineDTO();

                cineDTO.Nombre = item.Nombre;
                cineDTO.Id = item.Id;

                cinesDTO.Add(cineDTO);
            }

            var GenerosDTO = mapper.Map<List<GeneroDTO>>(generos);

            return new PeliculasPostGetDTO() { Cines = cinesDTO, Generos = GenerosDTO};
        }

        private void EscribirOrdenActores(Pelicula pelicula) 
        {
            if (pelicula.PeliculasActores !=  null)
            {
                for (int i = 0; i < pelicula.PeliculasActores.Count; i++)
                {
                    pelicula.PeliculasActores[i].Orden = i;
                }
            }
        }
    }
}
