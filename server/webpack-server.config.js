const path = require('path');
var fs = require('fs');
var CopyWebpackPlugin = require('copy-webpack-plugin');

var nodeModules = {};
fs.readdirSync('node_modules')
	.filter(function(x) {
		return ['.bin'].indexOf(x) === -1;
	})
	.forEach(function(mod) {
		nodeModules[mod] = 'commonjs ' + mod;
	});

module.exports = {
	target: 'node',
	node: {
		console: false,
		global: false,
		process: false,
		Buffer: false,
		__filename: true,
		__dirname: true
  },
  context: path.resolve('src'),
  entry: './lite-server.ts',
  externals: nodeModules,
  devtool: 'inline-source-map',
  plugins: [
    new CopyWebpackPlugin([
        { from: 'public', to: 'public' },
        { from: 'views', to: 'views' }
      ])
  ],
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/
      }
    ],
    loaders: [
      { test: /\.env$/, loader: "file" }
    ]
  },
  resolve: {
    extensions: [ ".tsx", ".ts", ".js" ]
  },
  output: {
    filename: 'lite-server-bundle.js',
    path: path.resolve(__dirname, 'dist')
  }
};
