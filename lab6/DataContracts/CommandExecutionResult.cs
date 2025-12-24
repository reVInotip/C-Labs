using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataContracts;

public record CommandExecutionResult(int PhilosopherId, bool ok);
