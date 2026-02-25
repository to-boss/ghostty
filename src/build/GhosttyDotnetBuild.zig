const Ghostty = @This();

const std = @import("std");
const builtin = @import("builtin");
const RunStep = std.Build.Step.Run;
const Config = @import("Config.zig");
const GhosttyLib = @import("GhosttyLib.zig");

build: *std.Build.Step.Run,
copy: *std.Build.Step.Run,

pub const Deps = struct {
    lib: *const GhosttyLib,
};

pub fn init(
    b: *std.Build,
    config: *const Config,
    deps: Deps,
) !Ghostty {
    _ = config;

    const dotnet_config = "Release";
    const project_dir = "windows/src/Ghostty";
    const output_dir = b.fmt("{s}/bin/{s}/net9.0-windows", .{ project_dir, dotnet_config });

    // Our step to build the Ghostty Windows app via dotnet build.
    const build_step = build: {
        const step = RunStep.create(b, "dotnet build");
        step.has_side_effects = true;
        step.cwd = b.path("windows");
        step.addArgs(&.{
            "dotnet",
            "build",
            "Ghostty.sln",
            "-c",
            dotnet_config,
        });

        // Depend on libghostty being built first
        step.step.dependOn(deps.lib.step);

        step.expectExitCode(0);
        break :build step;
    };

    // Our step to copy the built app to the install path.
    const copy = copy: {
        const step = RunStep.create(b, "copy windows app");
        step.has_side_effects = true;

        // Use xcopy on Windows to copy the output directory
        step.addArgs(&.{ "xcopy", "/E", "/I", "/Y" });
        step.addFileArg(b.path(output_dir));
        step.addArg(b.fmt("{s}/Ghostty", .{b.install_path}));
        step.step.dependOn(&build_step.step);
        break :copy step;
    };

    return .{
        .build = build_step,
        .copy = copy,
    };
}

pub fn install(self: *const Ghostty) void {
    const b = self.copy.step.owner;
    b.getInstallStep().dependOn(&self.copy.step);
}
