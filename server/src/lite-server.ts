/**
 * Module dependencies.
 */
import * as express from "express";
import * as compression from "compression";  // compresses requests
import * as bodyParser from "body-parser";
import * as logger from "morgan";
import * as errorHandler from "errorhandler";
import * as dotenv from "dotenv";
import * as path from "path";

/**
 * Load environment variables from .env file, where API keys and passwords are configured.
 */
let envPath = path.join(__dirname, 'config/app.env');
console.log("ENV PATH:" + envPath);
dotenv.config({ path: envPath});
let env = process.env.APP_ENV;
console.log("ENV:" + env);

/**
 * Controllers (route handlers).
 */
import * as homeController from "./controllers/home";
import * as homeApi from "./api/home";

/**
 * Create Express server.
 */
const app = express();

/**
 * Express configuration.
 */
app.set("port", process.env.PORT || 3000);
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');
app.use(compression());
app.use(logger("dev"));
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
let staticFilePath = path.join(__dirname, "public");
console.log("Static Path: " + staticFilePath);
app.use(express.static(staticFilePath, { maxAge: 31557600000 }));

/**
 * Primary app routes.
 */
app.get("/", homeController.index);
app.get("/local", homeController.local);

/**
 * API examples routes.
 */
app.get("/api", homeApi.getApi);

if (env === 'LOCAL') {
  let webpackMiddleware = require("webpack-dev-middleware");
  let webpack = require('webpack');
  let config = require('../webpack-client.config');

  app.use(webpackMiddleware(webpack(config), {
    publicPath: "/build",

    headers: { "X-Custom-Webpack-Header": "yes" },

    stats: {
      colors: true
    }
  }));

  /**
   * Error Handler. Provides full stack - remove for production
   */
  app.use(errorHandler());
}

/**
 * Start Express server.
 */
app.listen(app.get("port"), () => {
  console.log(("  App is running at http://localhost:%d in %s mode"), app.get("port"), app.get("env"));
  console.log("  Press CTRL-C to stop\n");
});

module.exports = app;
