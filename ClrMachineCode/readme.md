# ClrMachineCode

This project provides hardware-specific optimized versions of some arithmetic operations which are
important for the speed of some algorithms and libraries.


## Benchmarks

These numbers are from an Intel(R) Core(TM) i7 CPU         930  @ 2.80GHz.
They are run under .NET 4.6 in x64 mode.

| Test | Cycles/iteration |
| ------ |------:|
|popcnt32-software | 13 |
|popcnt32-native | 6 |
|popcnt32-adaptive | 10 |
|popcnt64-software | 11 |
|popcnt64-native | 9 |
|popcnt64-adaptive | 15 |
|empty loop | 3 |
|popcnt64-software 4x unrolled | 43 |
|popcnt64-native 4x unrolled | 20 |
|swapbytes32-software | 9 |
|swapbytes32-native | 6 |
|swapbytes32-adaptive | 6 |
|swapbytes64-software | 21 |
|swapbytes64-native | 6 |
|swapbytes64-adaptive | 6 |
|swapbytes64-software, 4x unrolled | 75 |
|swapbytes64-native, 4x unrolled | 21 |


## TODO
- Usability: Efficient, transparent runtime checks for whether to use optimized version. OK
- Stability: Support x86. OK
- Stability: Perform intel cpu feature checks. OK
- Stability: Perform cpu feature checks. OK
- Stability: Skip code replacement on environments which it might not work on: NGEN, arm, non-windows...


