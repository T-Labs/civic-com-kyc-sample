﻿using CivicLoginApi.Result;
using System;
using System.Collections.Generic;
using System.Text;

namespace CivicLoginApi.Helper
{
    public interface ICivicHelper
    {
        Result<List<CivicUser>> ExchangeCodeAsync(string jwt);
    }
}
