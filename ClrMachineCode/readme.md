# ClrMachineCode

This project provides hardware-specific optimized versions of some arithmetic operations which are
important for the speed of some algorithms and libraries.


## Benchmarks

These numbers are from my machine, an Intel(R) Core(TM) i7 CPU         930  @ 2.80GHz.
They are run under .NET 4.6 in x64 mode.

| Test | Cycles/iteration |
| ------ |------:|
|popcnt32-software | 13 |
|popcnt32-native | 6 |
|popcnt64-software | 11 |
|popcnt64-native | 6 |
|empty loop | 3 |
|popcnt64-software 4x unrolled | 42 |
|popcnt64-native 4x unrolled | 20 |
|swapbytes32-software | 9 |
|swapbytes32-native | 6 |
|swapbytes64-software | 21 |
|swapbytes64-native | 6 |
|swapbytes64-software, 4x unrolled | 74 |
|swapbytes64-native, 4x unrolled | 21 |

