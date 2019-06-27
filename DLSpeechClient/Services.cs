// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Jot;

    internal static class Services
    {
        private static StateTracker tracker = new StateTracker();

        public static StateTracker Tracker { get => tracker; set => tracker = value; }
    }
}
