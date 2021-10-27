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

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] PeliculaCreacionDTO peliculaCreacionDTO) // SI SE ARMA. SIGUE CON EL CURSO
        {
            Pelicula pelicula = mapper.Map<Pelicula>(peliculaCreacionDTO);

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
