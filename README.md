# BmsCan

E14 / 88122 BMS CAN 通讯工位。用 Visual Studio 打开 `BmsCan通讯.sln`。

## 以后怎么提交

**只提交源码改动**，不要把 dll、exe、厂商库一起推上去。

可以提交：

- `.cs`、`.resx`、`.csproj`、`.sln`、`App.config`
- `README.md`、`.gitignore`
- `Properties/`

不要提交：

- `bin/`、`obj/`、`.vs/`、`.pdb`、`.user`
- `ControlCAN.dll`、`lib/` 里的厂商库和 CAN 驱动
- `SqlHelper.cs`（本机数据库账号，用 `SqlHelper.example.cs`）

本仓库已用 `.gitignore` 挡住上面这些。提交前看一眼 `git status`，列表里应只有你改过的代码文件。