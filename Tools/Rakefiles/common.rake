# frozen_string_literal: true

# 共通処理、設定等

BUILD_VERSION = '0.1.0'

TARGET_PLATFORM_ALIASES = {
  'common' => 'common',
  'win' => 'StandaloneWindows64',
  'windows' => 'StandaloneWindows64',
  'standalonewindows' => 'StandaloneWindows64',
  'standalonewindows64' => 'StandaloneWindows64',
  'ios' => 'iOS',
  'android' => 'Android',
  'switch' => 'Switch'
}.freeze

COMMANDLINE_BUILD_TARGETS = {
  'StandaloneWindows64' => 'win64',
  'iOS' => 'ios',
  'Android' => 'android',
  'Switch' => 'switch'
}.freeze

TARGET_PLATFORMS = %w[
  StandaloneWindows64
  Android
  iOS
  Switch
].freeze

#==========================================================
# 便利関数群
#==========================================================

def get_target_platform(name)
  platform = TARGET_PLATFORM_ALIASES[name.downcase]
  platform || raise("不正なプラットフォームです, platform=#{name}。 有効な指定は #{TARGET_PLATFORM_ALIASES.values.join(',')} です")
end

def get_asset_bundle_platform(platform)
  platform = 'StandaloneWindows' if platform == 'StandaloneWindows64'
  platform
end

def get_branch_name
  (ENV['GIT_BRANCH'] || `git rev-parse --abbrev-ref HEAD`.strip).gsub(%r{^origin/}, '')
end

class String
  def win_path
    tr('/', '\\')
  end
end
