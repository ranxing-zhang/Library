﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Library.API.Configs;
using Library.API.Entities;
using Library.API.Extentions;
using Library.API.Service.Interface;
using Library.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,SuperAdministrator")]
    public class CategoryController : ControllerBase
    {
        #region field

        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly Dictionary<string, PropertyMapping> _mappingDict;

        #endregion

        #region ctor

        public CategoryController(IServicesWrapper servicesWrapper, IMapper mapper)
        {
            _categoryService = servicesWrapper.Category;
            _mapper = mapper;
            _mappingDict = new Dictionary<string, PropertyMapping>()
            {
                {"id", new PropertyMapping("Id")},
                {"name", new PropertyMapping("Name")},
            };
        }

        #endregion

        #region Get

        // GET: api/<CategoryController>
        [HttpGet]
        public async Task<ActionResult<PagedList<CategoryDto>>> Get(string sort = "id", string? search = null,
            int page = 1, int pageSize = 25)
        {
            IQueryable<Category>? categories;
            if (search == null)
            {
                categories = await _categoryService.GetAllAsync();
            }
            else
            {
                categories = await _categoryService.GetByConditionAsync(category => category.Name.Contains(search));
            }

            categories = categories.Sort(sort, _mappingDict);
            return await PagedList<CategoryDto>.CreateAsync(
                categories.ProjectTo<CategoryDto>(_mapper.ConfigurationProvider), page, pageSize);
        }

        // GET api/<CategoryController>/5
        [HttpGet("{id:guid}", Name = nameof(Get))]
        public async Task<ActionResult<CategoryDto>> Get(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return _mapper.Map<CategoryDto>(category);
        }

        #endregion

        #region Post

        // POST api/<CategoryController>
        [HttpPost]
        public async Task<IActionResult> Post(CategoryCreateDto dto)
        {
            var result = await _categoryService.AddAsync(_mapper.Map<Category>(dto));
            if (result == null)
            {
                throw new Exception("创建资源Category失败！");
            }

            var vo = _mapper.Map<CategoryDto>(result);
            return CreatedAtRoute(nameof(Get), new { id = result.Id }, vo);
        }

        #endregion

        #region Put

        // PUT api/<CategoryController>/5
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> PutAsync(Guid id, [FromBody] CategoryCreateDto dto)
        {
            var category = await _categoryService.GetByIdAsync(id);

            _mapper.Map(dto, category);
            await _categoryService.UpdateAsync(category);
            return NoContent();
        }

        #endregion

        #region Delete

        // DELETE api/<CategoryController>/5
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            await _categoryService.DeleteAsync(category);
            return NoContent();
        }

        #endregion
    }
}