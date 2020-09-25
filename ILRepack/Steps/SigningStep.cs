//
// Copyright (c) 2015 Francois Valdy
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ILRepacking.Steps
{
    class SigningStep : IRepackStep
    {
        readonly IRepackContext _repackContext;
        readonly RepackOptions _repackOptions;

        public byte[] KeyBlob { get; private set; }

        public SigningStep(
            IRepackContext repackContext,
            RepackOptions repackOptions)
        {
            _repackContext = repackContext;
            _repackOptions = repackOptions;
        }

        public void Perform()
        {
            if (_repackOptions.KeyContainer != null)
            {
                throw new NotImplementedException("Signing with KeyContainer was not supported.");
            }
            
            if (_repackOptions.DelaySign)
            {
                throw new NotImplementedException("DelaySign was not supported.");
            }
            if (_repackOptions.KeyFile != null)
            {
                if (!File.Exists(_repackOptions.KeyFile))
                    throw new IOException("KeyFile doesn't exist.");

                KeyBlob = File.ReadAllBytes(_repackOptions.KeyFile);
            }
            else
            {
                _repackContext.TargetAssemblyDefinition.Name.PublicKey = null;
                _repackContext.TargetAssemblyMainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
            }
        }
    }
}
