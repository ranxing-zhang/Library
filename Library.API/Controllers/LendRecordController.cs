﻿using AutoMapper;
using Library.API.Configs;
using Library.API.Configs.Filters;
using Library.API.Entities;
using Library.API.Extentions;
using Library.API.Helper;
using Library.Common.Models;
using Library.API.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LendRecordController : ControllerBase
    {
        #region field
        private readonly ILendRecordService _lendRecordService;
        private readonly UserManager<User> _userManager;
        private readonly IBookService _bookService;
        private readonly ILendConfigService _lendConfigService;
        private readonly IMapper _mapper;
        private readonly HashFactory _hashFactory;
        private readonly Dictionary<string, PropertyMapping> mappingDict;
        #endregion

        #region ctor

        public LendRecordController(IServicesWrapper serviceWrapper, IMapper mapper, HashFactory hashFactory, UserManager<User> userManager)
        {
            _lendConfigService = serviceWrapper.LendConfig;
            _lendRecordService = serviceWrapper.LendRecord;
            _bookService = serviceWrapper.Book;
            _userManager = userManager;
            _mapper = mapper;
            _hashFactory = hashFactory;
            mappingDict = new Dictionary<string, PropertyMapping>
            {
                {"id",new PropertyMapping("Id")},
                {"userId",new PropertyMapping("UserId") },
                {"bookId",new PropertyMapping("BookId") },
                {"lendTime",new PropertyMapping("StartTime") },
                {"returnTime",new PropertyMapping("RealReturnTime") },
            };
        }
        #endregion

        #region Get

        [HttpGet(Name = nameof(GetLendRecordsAsync))]
        public async Task<ActionResult<PagedList<LendRecordVo>>> GetLendRecordsAsync(string sort = "id", Guid? userId = null, string? lendTime = null, string? returnTime = null, int page = 1, int pageSize = 25)
        {
            var records = await _lendRecordService.GetAllAsync();
            Expression<Func<LendRecord, bool>>? select = default;
            if (userId != null)
            {
                select = record => record.UserId == userId;
            }
            if (lendTime != null)
            {
                if (select == null)
                {
                    select = record => record.StartTime.Date == DateTime.Parse(lendTime);
                }
                else
                {
                    select = select.And(record => record.StartTime.Date == DateTime.Parse(lendTime));
                }
            }
            if (returnTime != null)
            {
                if (select == null)
                {
                    select = record => record.StartTime.Date == DateTime.Parse(returnTime);
                }
                else
                {
                    select = select.And(record => record.StartTime.Date == DateTime.Parse(returnTime));
                }
            }
            if (select != null)
            {
                records = records.Where(select);
            }
            records = records.Sort(sort, mappingDict);
            return await PagedList<LendRecordVo>.CreateAsync(_mapper.ProjectTo<LendRecordVo>(records), page, pageSize);
        }

        [HttpGet("{id}", Name = nameof(GetRecordAsync))]
        public async Task<ActionResult<LendRecordVo>> GetRecordAsync(Guid id)
        {
            var record = await _lendRecordService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound();
            }
            var entityNewHash = _hashFactory.GetHash(record);
            Response.Headers[HeaderNames.ETag] = entityNewHash;
            return _mapper.Map<LendRecordVo>(record);
        }
        #endregion

        #region Post

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdministrator")]
        public async Task<IActionResult> AddRecordAsync(LendRecordForCreationDto dto)
        {
            var book = await _bookService.GetByIdAsync(dto.BookId);
            if (book == null)
            {
                throw new Exception($"Id: {dto.BookId}的书籍不存在！");
            }
            if (book.IsLend)
            {
                throw new Exception("该书籍已经被租借！");
            }
            var lendRecord = _mapper.Map<LendRecord>(dto);
            lendRecord.StartTime = DateTime.Now;
            var token = new JwtSecurityToken(Request.Headers.Authorization[0].Substring("Bearer ".Length));
            var email = token.Claims.Where(claim => claim.Type == JwtRegisteredClaimNames.Email).FirstOrDefault()!.Value;
            var processer = await _userManager.FindByEmailAsync(email);
            lendRecord.Processer = new Guid(processer.Id);
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user.Grade == null)
            {
                throw new Exception("用户没有对应借阅规则，请设置用户等级");
            }
            var lendConfigs = await _lendConfigService.GetByConditionAsync(config => config.ReaderGrade == user.Grade);
            var config = lendConfigs.FirstOrDefault();
            var maxCount = config!.MaxLendNumber;
            var realCount = await _lendRecordService.CountByConditionAsync(record => record.UserId == dto.UserId && record.RealReturnTime == DateTime.MinValue);
            if (maxCount < realCount)
            {
                throw new Exception("当前借阅数量超过最大借阅数目，请先归还先前借阅书籍！");
            }
            lendRecord.EndTime = DateTime.Now.AddDays(config.MaxLendDays);
            var result = await _lendRecordService.AddAsync(lendRecord);
            if (result == null)
            {
                throw new Exception("租借失败，请稍后再试！");
            }
            else
            {
                book.IsLend = true;
                await _bookService.UpdateAsync(book);
            }
            var vo = _mapper.Map<LendRecordVo>(result);
            return CreatedAtAction(nameof(GetRecordAsync), new { id = vo.Id }, vo);
        }
        #endregion

        #region Put

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdministrator")]
        [CheckIfMatchHeaderFilter]
        public async Task<IActionResult> PutAsync(Guid id)
        {
            var record = await _lendRecordService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound();
            }
            var entityHash = _hashFactory.GetHash(record);
            if (Request.Headers.TryGetValue(HeaderNames.IfMatch, out var requestETag) && requestETag != entityHash)
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            record.RealReturnTime = DateTime.Now;
            await _lendRecordService.UpdateAsync(record);
            var entityNewHash = _hashFactory.GetHash(record);
            Response.Headers[HeaderNames.ETag] = entityNewHash;
            return NoContent();
        }
        #endregion
    }
}
