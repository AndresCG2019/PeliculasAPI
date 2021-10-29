using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PeliculasController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly UserManager<IdentityUser> userManager;

        public PeliculasController(ApplicationDbContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos,
            UserManager<IdentityUser> userManager) 
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
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
        [AllowAnonymous]
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

                var promedioVoto = 0.0;
                var votoUsuario = 0;

                if (await context.Ratings.AnyAsync(x => x.PeliculaId == Id))
                {
                    promedioVoto = await context.Ratings.Where(x => x.PeliculaId == Id).AverageAsync(x => x.Puntuacion);

                    if (HttpContext.User.Identity.IsAuthenticated)
                    {
                        var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email").Value;
                        var usuario = await userManager.FindByEmailAsync(email);
                        var usuarioId = usuario.Id;
                        var ratingDB = await context.Ratings
                            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.PeliculaId == Id);

                        if (ratingDB != null)
                        {
                            votoUsuario = ratingDB.Puntuacion;
                        }
                    }
                }

                PeliculaDTO dto = mapper.Map<PeliculaDTO>(pelicula); // a dto despues de esto le faltan actores y cines
                dto.PromedioVoto = promedioVoto;
                dto.votoUsuario = votoUsuario;

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

        [HttpGet("filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PeliculaDTO>>> Filtrar([FromQuery] PeliculasFiltrarDTO peliculasFiltrarDTO)
        {
            var peliculasQueryable = context.Peliculas.AsQueryable();

            if (!string.IsNullOrEmpty(peliculasFiltrarDTO.Titulo))
            {
                peliculasQueryable = peliculasQueryable.Where(x => x.Titulo.Contains(peliculasFiltrarDTO.Titulo));
            }

            if (peliculasFiltrarDTO.EnCines)
            {
                peliculasQueryable = peliculasQueryable.Where(x => x.EnCines);
            }

            if (peliculasFiltrarDTO.ProximosEstrenos)
            {
                var hoy = DateTime.Today;
                peliculasQueryable = peliculasQueryable.Where(x => x.FechaLanzamiento > hoy);
            }

            if (peliculasFiltrarDTO.GeneroId != 0)
            {
                peliculasQueryable = peliculasQueryable
                    .Where(x => x.PeliculasGeneros.Select(y => y.GeneroId)
                    .Contains(peliculasFiltrarDTO.GeneroId));
            }

            await HttpContext.InsertarParametrosPaginacionEnCabecera(peliculasQueryable);

            var peliculas = await peliculasQueryable.Paginar(peliculasFiltrarDTO.PaginacionDTO).ToListAsync();
            return mapper.Map<List<PeliculaDTO>>(peliculas);
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

        [HttpGet("PutGet/{id:int}")]
        public async Task<ActionResult<PeliculasPutGetDTO>> PutGet(int id)
        {
            //EDITAR PELICULAS NO FUNCIONA. AVANZAR CON EL CURSO
            
            var peliculaActionResult = await Get(id);
            if (peliculaActionResult.Result is NotFoundResult) { return NotFound(); }

            var pelicula = peliculaActionResult.Value;

            var generosSeleccionadosIds = pelicula.Generos.Select(x => x.Id).ToList();
            var generosNoSeleccionados = await context.Generos
                .Where(x => !generosSeleccionadosIds.Contains(x.Id))
                .ToListAsync();

            var cinesSeleccionadosIds = pelicula.Cines.Select(x => x.Id).ToList();
            var cinesNoSeleccionados = await context.Cines
                .Where(x => !cinesSeleccionadosIds.Contains(x.Id))
                .ToListAsync();

            var generosNoSeleccionadosDTO = mapper.Map<List<GeneroDTO>>(generosNoSeleccionados);
            //var cinesNoSeleccionadosDTO = mapper.Map<List<CineDTO>>(cinesNoSeleccionados);

            var respuesta = new PeliculasPutGetDTO();
            respuesta.Pelicula = pelicula;
            respuesta.GenerosSeleccionados = pelicula.Generos;
            respuesta.GenerosNoSeleccionados = generosNoSeleccionadosDTO;
            respuesta.CinesSeleccionados = pelicula.Cines;
            //respuesta.CinesNoSeleccionados = cinesNoSeleccionadosDTO;
            //respuesta.Actores = pelicula.Actores;
            return respuesta;
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = await context.Peliculas
                .Include(x => x.PeliculasActores)
                .Include(x => x.PeliculasGeneros)
                .Include(x => x.PeliculasCines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }

            pelicula = mapper.Map(peliculaCreacionDTO, pelicula);

            //if (peliculaCreacionDTO.Poster != null)
            //{
            //    pelicula.Poster = await almacenadorArchivos.EditarArchivo(contenedor, peliculaCreacionDTO.Poster, pelicula.Poster);
            //}

            EscribirOrdenActores(pelicula);

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var pelicula = await context.Peliculas.FirstOrDefaultAsync(x => x.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }

            context.Remove(pelicula);
            await context.SaveChangesAsync();
            //await almacenadorArchivos.BorrarArchivo(pelicula.Poster, contenedor);
            return NoContent();
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
