using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/cines")]
    public class CinesController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public CinesController(ApplicationDbContext context, IMapper mapper) 
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet] // api/cines
        public async Task<ActionResult<List<CineDTO>>> Get()
        {
            List<Cine> cines = await context.Cines.ToListAsync();
            List<CineDTO> cinesDTO = new List<CineDTO>();

            foreach (var item in cines)
            {
                CineDTO cineDTO = new CineDTO();

                cineDTO.Nombre = item.Nombre;
                cineDTO.Id = item.Id;

                cinesDTO.Add(cineDTO);
            }

            return cinesDTO;
        }

        [HttpGet("{Id:int}")] // api/generos/3/
        public async Task<ActionResult<CineDTO>> Get(int Id)
        {
            Cine cine = await context.Cines.FirstOrDefaultAsync(x => x.Id == Id);

            if (cine == null)
            {
                return NotFound();
            }

            CineDTO cineDTO = new CineDTO() { Nombre = cine.Nombre, Id = cine.Id};

            return cineDTO;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CineCreacionDTO cineCreacionDTO) 
        {
            Cine cine = new Cine() { Nombre = cineCreacionDTO.Nombre};

            context.Add(cine);

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromBody] CineCreacionDTO cineCreacionDTO)
        {
            Cine cine = await context.Cines.FirstOrDefaultAsync(x => x.Id == id);

            if (cine == null)
            {
                return NotFound();
            }

            cine.Nombre = cineCreacionDTO.Nombre;

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            Cine cine = await context.Cines.FirstOrDefaultAsync(x => x.Id == id);

            if (cine == null)
            {
                return NotFound();
            }

            context.Remove(cine);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
