﻿using Library.API.Entities;
using Library.API.Repository.Interface;
using Library.API.Service.Interface;
using System;

namespace Library.API.Service
{
    public class LendConfigService : BaseService<LendConfig, Guid>, ILendConfigService
    {
        public LendConfigService(IBaseRepository<LendConfig, Guid> bal)
        {
            BaseDal = bal;
        }
    }
}
